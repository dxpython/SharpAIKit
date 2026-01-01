"""
High-level Agent API for SharpAIKit Python SDK
"""

import logging
from typing import List, Optional, Dict, Any, Iterator
import uuid

from .client import GrpcClient
from .process import HostProcessManager
from .models import ExecutionResult, SkillInfo
from .errors import (
    SharpAIKitError,
    AgentNotFoundError,
    ExecutionError,
    ConnectionError,
    HostStartupError,
)

logger = logging.getLogger(__name__)


class Agent:
    """
    High-level Agent API for SharpAIKit

    This class provides a simple interface for creating and executing agents
    with automatic host process management.
    """

    def __init__(
        self,
        api_key: str,
        model: str = "gpt-3.5-turbo",
        base_url: str = "https://api.openai.com/v1",
        skills: Optional[List[str]] = None,
        agent_id: Optional[str] = None,
        host: str = "localhost",
        port: int = 50051,
        auto_start_host: bool = True,
        host_executable: Optional[str] = None,
        host_working_dir: Optional[str] = None,
    ):
        """
        Initialize an Agent instance

        Args:
            api_key: LLM API key
            model: Model name
            base_url: LLM base URL
            skills: List of skill IDs to activate
            agent_id: Unique agent identifier (auto-generated if None)
            host: gRPC host address
            port: gRPC port
            auto_start_host: Automatically start host process if not running
            host_executable: Path to host executable (auto-detected if None)
            host_working_dir: Working directory for host (auto-detected if None)
        """
        self.api_key = api_key
        self.model = model
        self.base_url = base_url
        self.skills = skills or []
        self.agent_id = agent_id or str(uuid.uuid4())
        self.host = host
        self.port = port

        # Initialize process manager
        self._process_manager = HostProcessManager(
            host=host,
            port=port,
            host_executable=host_executable,
            host_working_dir=host_working_dir,
        )

        # Initialize gRPC client
        self._client = GrpcClient(host=host, port=port)

        # Auto-start host if requested
        self._host_started_by_us = False
        if auto_start_host:
            if not self._process_manager.is_running():
                logger.info("Starting SharpAIKit host process...")
                self._process_manager.start()
                self._host_started_by_us = True
            else:
                logger.info("SharpAIKit host already running")

        # Create agent on host
        try:
            self._client.create_agent(
                agent_id=self.agent_id,
                api_key=self.api_key,
                base_url=self.base_url,
                model=self.model,
                skill_ids=self.skills,
            )
            logger.info(f"Agent created: {self.agent_id}")
        except Exception as e:
            if self._host_started_by_us:
                self._process_manager.stop()
            raise SharpAIKitError(f"Failed to create agent: {str(e)}") from e

    def run(
        self,
        task: str,
        tools: Optional[List[Dict[str, Any]]] = None,
        context: Optional[Dict[str, str]] = None,
    ) -> ExecutionResult:
        """
        Execute an agent task (synchronous)

        Args:
            task: Task description
            tools: Optional list of tool definitions
            context: Optional context dictionary

        Returns:
            ExecutionResult

        Raises:
            ExecutionError: If execution fails
            AgentNotFoundError: If agent is not found
            ConnectionError: If connection fails
        """
        try:
            return self._client.execute(
                agent_id=self.agent_id,
                task=task,
                tools=tools,
                context=context,
            )
        except Exception as e:
            if isinstance(e, (ExecutionError, AgentNotFoundError, ConnectionError)):
                raise
            raise ExecutionError(f"Unexpected error: {str(e)}") from e

    def run_stream(
        self,
        task: str,
        tools: Optional[List[Dict[str, Any]]] = None,
        context: Optional[Dict[str, str]] = None,
    ) -> Iterator[ExecutionResult]:
        """
        Execute an agent task with streaming results

        Args:
            task: Task description
            tools: Optional list of tool definitions
            context: Optional context dictionary

        Yields:
            ExecutionResult chunks

        Raises:
            ExecutionError: If execution fails
            AgentNotFoundError: If agent is not found
            ConnectionError: If connection fails
        """
        try:
            yield from self._client.execute_stream(
                agent_id=self.agent_id,
                task=task,
                tools=tools,
                context=context,
            )
        except Exception as e:
            if isinstance(e, (ExecutionError, AgentNotFoundError, ConnectionError)):
                raise
            raise ExecutionError(f"Unexpected error: {str(e)}") from e

    async def run_async(
        self,
        task: str,
        tools: Optional[List[Dict[str, Any]]] = None,
        context: Optional[Dict[str, str]] = None,
    ) -> ExecutionResult:
        """
        Execute an agent task (asynchronous)

        Args:
            task: Task description
            tools: Optional list of tool definitions
            context: Optional context dictionary

        Returns:
            ExecutionResult

        Raises:
            ExecutionError: If execution fails
            AgentNotFoundError: If agent is not found
            ConnectionError: If connection fails
        """
        import asyncio

        loop = asyncio.get_event_loop()
        return await loop.run_in_executor(None, self.run, task, tools, context)

    def get_skill_resolution(self) -> Optional[ExecutionResult]:
        """
        Get last skill resolution for this agent

        Returns:
            SkillResolution or None if not available
        """
        try:
            skill_resolution = self._client.get_last_skill_resolution(self.agent_id)
            # Convert to ExecutionResult-like structure for consistency
            from .models import ExecutionResult, SkillResolution

            return ExecutionResult(
                output="",
                success=True,
                skill_resolution=skill_resolution,
            )
        except Exception as e:
            logger.warning(f"Failed to get skill resolution: {e}")
            return None

    def list_available_skills(self) -> List[SkillInfo]:
        """
        List all available skills

        Returns:
            List of SkillInfo
        """
        return self._client.list_available_skills()

    def close(self) -> None:
        """Close the agent and cleanup resources"""
        self._client.close()
        if self._host_started_by_us:
            self._process_manager.stop()

    def __enter__(self):
        """Context manager entry"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit"""
        self.close()

    def __del__(self):
        """Cleanup on deletion"""
        try:
            self.close()
        except Exception:
            pass

