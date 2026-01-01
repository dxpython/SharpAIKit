"""
gRPC client for SharpAIKit
"""

import grpc
import logging
from typing import List, Optional, Dict, Any, Iterator
import uuid

from ._grpc import sharpaikit_pb2, sharpaikit_pb2_grpc
from .models import ExecutionResult, ExecutionStep, SkillResolution, SkillConstraints, SkillInfo
from .errors import (
    ConnectionError,
    AgentNotFoundError,
    ExecutionError,
    SkillResolutionError,
    SharpAIKitError,
)

logger = logging.getLogger(__name__)


class GrpcClient:
    """gRPC client for communicating with SharpAIKit host"""

    def __init__(self, host: str = "localhost", port: int = 50051):
        """
        Initialize gRPC client

        Args:
            host: Host address
            port: gRPC port
        """
        self.host = host
        self.port = port
        self._channel: Optional[grpc.Channel] = None
        self._stub: Optional[sharpaikit_pb2_grpc.AgentServiceStub] = None

    def _ensure_connected(self) -> None:
        """Ensure gRPC channel is connected"""
        if self._channel is None:
            try:
                target = f"{self.host}:{self.port}"
                self._channel = grpc.insecure_channel(target)
                self._stub = sharpaikit_pb2_grpc.AgentServiceStub(self._channel)

                # Wait for channel to be ready (with timeout)
                try:
                    grpc.channel_ready_future(self._channel).result(timeout=5)
                except grpc.FutureTimeoutError:
                    raise ConnectionError(f"Failed to connect to {target}")

            except Exception as e:
                if isinstance(e, ConnectionError):
                    raise
                raise ConnectionError(f"Failed to connect to {self.host}:{self.port}: {str(e)}") from e

    def create_agent(
        self,
        agent_id: Optional[str] = None,
        api_key: str = "",
        base_url: str = "https://api.openai.com/v1",
        model: str = "gpt-3.5-turbo",
        skill_ids: Optional[List[str]] = None,
        options: Optional[Dict[str, str]] = None,
    ) -> str:
        """
        Create a new agent instance

        Args:
            agent_id: Unique agent identifier (auto-generated if None)
            api_key: LLM API key
            base_url: LLM base URL
            model: Model name
            skill_ids: List of skill IDs to activate
            options: Additional options

        Returns:
            Agent ID
        """
        self._ensure_connected()

        if agent_id is None:
            agent_id = str(uuid.uuid4())

        request = sharpaikit_pb2.CreateAgentRequest(
            agent_id=agent_id,
            api_key=api_key,
            base_url=base_url,
            model=model,
            skill_ids=skill_ids or [],
            options=options or {},
        )

        try:
            response = self._stub.CreateAgent(request)
            if not response.success:
                raise SharpAIKitError(response.error, "CREATE_AGENT_ERROR")
            return response.agent_id
        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                raise ConnectionError(f"Host unavailable: {e.details()}") from e
            raise SharpAIKitError(f"gRPC error: {e.details()}", "CREATE_AGENT_ERROR") from e

    def execute(
        self,
        agent_id: str,
        task: str,
        tools: Optional[List[Dict[str, Any]]] = None,
        context: Optional[Dict[str, str]] = None,
    ) -> ExecutionResult:
        """
        Execute an agent task

        Args:
            agent_id: Agent identifier
            task: Task description
            tools: Optional list of tool definitions
            context: Optional context dictionary

        Returns:
            ExecutionResult
        """
        self._ensure_connected()

        request = sharpaikit_pb2.ExecuteRequest(
            agent_id=agent_id,
            task=task,
            context=context or {},
        )

        if tools:
            for tool in tools:
                tool_def = sharpaikit_pb2.ToolDefinition(
                    name=tool.get("name", ""),
                    description=tool.get("description", ""),
                )
                if "parameters" in tool:
                    for param in tool["parameters"]:
                        tool_param = tool_def.parameters.add()
                        tool_param.name = param.get("name", "")
                        tool_param.type = param.get("type", "string")
                        tool_param.description = param.get("description", "")
                        tool_param.required = param.get("required", True)
                request.tools.append(tool_def)

        try:
            response = self._stub.Execute(request)

            if not response.success:
                error_code = response.error_code or "EXECUTION_ERROR"
                if error_code == "AGENT_NOT_FOUND":
                    raise AgentNotFoundError(agent_id)
                raise ExecutionError(
                    response.error or "Execution failed",
                    error_code,
                    list(response.denied_tools),
                )

            # Convert response to ExecutionResult
            steps = []
            for step in response.steps:
                steps.append(
                    ExecutionStep(
                        step_number=step.step_number,
                        type=step.type,
                        thought=step.thought if step.thought else None,
                        action=step.action if step.action else None,
                        observation=step.observation if step.observation else None,
                        tool_name=step.tool_name if step.tool_name else None,
                        tool_args=dict(step.tool_args),
                    )
                )

            skill_resolution = None
            if response.HasField("skill_resolution"):
                sr = response.skill_resolution
                constraints = None
                if sr.HasField("constraints"):
                    c = sr.constraints
                    constraints = SkillConstraints(
                        allowed_tools=list(c.allowed_tools),
                        forbidden_tools=list(c.forbidden_tools),
                        max_steps=c.max_steps if c.max_steps > 0 else None,
                        max_execution_time_ms=c.max_execution_time_ms if c.max_execution_time_ms > 0 else None,
                    )
                skill_resolution = SkillResolution(
                    activated_skill_ids=list(sr.activated_skill_ids),
                    decision_reasons=list(sr.decision_reasons),
                    constraints=constraints,
                    tool_denial_reasons=dict(sr.tool_denial_reasons),
                )

            return ExecutionResult(
                output=response.output,
                success=response.success,
                steps=steps,
                skill_resolution=skill_resolution,
                denied_tools=list(response.denied_tools),
            )

        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.NOT_FOUND:
                raise AgentNotFoundError(agent_id) from e
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                raise ConnectionError(f"Host unavailable: {e.details()}") from e
            raise ExecutionError(f"gRPC error: {e.details()}", "EXECUTION_ERROR") from e

    def execute_stream(
        self,
        agent_id: str,
        task: str,
        tools: Optional[List[Dict[str, Any]]] = None,
        context: Optional[Dict[str, str]] = None,
    ) -> Iterator[ExecutionResult]:
        """
        Execute an agent task with streaming results

        Args:
            agent_id: Agent identifier
            task: Task description
            tools: Optional list of tool definitions
            context: Optional context dictionary

        Yields:
            ExecutionResult chunks
        """
        self._ensure_connected()

        request = sharpaikit_pb2.ExecuteRequest(
            agent_id=agent_id,
            task=task,
            context=context or {},
        )

        if tools:
            for tool in tools:
                tool_def = sharpaikit_pb2.ToolDefinition(
                    name=tool.get("name", ""),
                    description=tool.get("description", ""),
                )
                if "parameters" in tool:
                    for param in tool["parameters"]:
                        tool_param = tool_def.parameters.add()
                        tool_param.name = param.get("name", "")
                        tool_param.type = param.get("type", "string")
                        tool_param.description = param.get("description", "")
                        tool_param.required = param.get("required", True)
                request.tools.append(tool_def)

        try:
            for chunk in self._stub.ExecuteStream(request):
                if chunk.HasField("text_chunk"):
                    yield ExecutionResult(output=chunk.text_chunk, success=True)
                elif chunk.HasField("step"):
                    step = chunk.step
                    yield ExecutionResult(
                        output="",
                        success=True,
                        steps=[
                            ExecutionStep(
                                step_number=step.step_number,
                                type=step.type,
                                thought=step.thought if step.thought else None,
                                action=step.action if step.action else None,
                                observation=step.observation if step.observation else None,
                                tool_name=step.tool_name if step.tool_name else None,
                                tool_args=dict(step.tool_args),
                            )
                        ],
                    )
                elif chunk.HasField("skill_resolution"):
                    sr = chunk.skill_resolution
                    constraints = None
                    if sr.HasField("constraints"):
                        c = sr.constraints
                        constraints = SkillConstraints(
                            allowed_tools=list(c.allowed_tools),
                            forbidden_tools=list(c.forbidden_tools),
                            max_steps=c.max_steps if c.max_steps > 0 else None,
                            max_execution_time_ms=c.max_execution_time_ms if c.max_execution_time_ms > 0 else None,
                        )
                    skill_resolution = SkillResolution(
                        activated_skill_ids=list(sr.activated_skill_ids),
                        decision_reasons=list(sr.decision_reasons),
                        constraints=constraints,
                        tool_denial_reasons=dict(sr.tool_denial_reasons),
                    )
                    yield ExecutionResult(
                        output="",
                        success=True,
                        skill_resolution=skill_resolution,
                    )
                elif chunk.HasField("error"):
                    raise ExecutionError(chunk.error, "EXECUTION_ERROR")
                elif chunk.done:
                    break

        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.NOT_FOUND:
                raise AgentNotFoundError(agent_id) from e
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                raise ConnectionError(f"Host unavailable: {e.details()}") from e
            raise ExecutionError(f"gRPC error: {e.details()}", "EXECUTION_ERROR") from e

    def list_available_skills(self) -> List[SkillInfo]:
        """
        List all available skills

        Returns:
            List of SkillInfo
        """
        self._ensure_connected()

        try:
            response = self._stub.ListAvailableSkills(sharpaikit_pb2.ListSkillsRequest())
            skills = []
            for skill in response.skills:
                skills.append(
                    SkillInfo(
                        id=skill.id,
                        name=skill.name,
                        description=skill.description,
                        version=skill.version,
                        priority=skill.priority,
                        scope=skill.scope,
                    )
                )
            return skills
        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                raise ConnectionError(f"Host unavailable: {e.details()}") from e
            raise SkillResolutionError(f"gRPC error: {e.details()}") from e

    def get_last_skill_resolution(self, agent_id: str) -> SkillResolution:
        """
        Get last skill resolution for an agent

        Args:
            agent_id: Agent identifier

        Returns:
            SkillResolution
        """
        self._ensure_connected()

        try:
            response = self._stub.GetLastSkillResolution(
                sharpaikit_pb2.GetSkillResolutionRequest(agent_id=agent_id)
            )

            if not response.success:
                raise SkillResolutionError(response.error or "Failed to get skill resolution")

            sr = response.skill_resolution
            constraints = None
            if sr.HasField("constraints"):
                c = sr.constraints
                constraints = SkillConstraints(
                    allowed_tools=list(c.allowed_tools),
                    forbidden_tools=list(c.forbidden_tools),
                    max_steps=c.max_steps if c.max_steps > 0 else None,
                    max_execution_time_ms=c.max_execution_time_ms if c.max_execution_time_ms > 0 else None,
                )

            return SkillResolution(
                activated_skill_ids=list(sr.activated_skill_ids),
                decision_reasons=list(sr.decision_reasons),
                constraints=constraints,
                tool_denial_reasons=dict(sr.tool_denial_reasons),
            )

        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.NOT_FOUND:
                raise AgentNotFoundError(agent_id) from e
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                raise ConnectionError(f"Host unavailable: {e.details()}") from e
            raise SkillResolutionError(f"gRPC error: {e.details()}") from e

    def health_check(self) -> bool:
        """
        Check if host is healthy

        Returns:
            True if healthy
        """
        self._ensure_connected()

        try:
            response = self._stub.HealthCheck(sharpaikit_pb2.HealthCheckRequest())
            return response.healthy
        except grpc.RpcError:
            return False

    def close(self) -> None:
        """Close the gRPC channel"""
        if self._channel:
            self._channel.close()
            self._channel = None
            self._stub = None

    def __enter__(self):
        """Context manager entry"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit"""
        self.close()

