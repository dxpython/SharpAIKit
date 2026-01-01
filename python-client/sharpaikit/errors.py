"""
Error classes for SharpAIKit Python SDK
"""


class SharpAIKitError(Exception):
    """Base exception for all SharpAIKit errors"""

    def __init__(self, message: str, error_code: str = None):
        super().__init__(message)
        self.message = message
        self.error_code = error_code


class ConnectionError(SharpAIKitError):
    """Raised when connection to gRPC host fails"""

    def __init__(self, message: str = "Failed to connect to SharpAIKit host"):
        super().__init__(message, "CONNECTION_ERROR")


class HostStartupError(SharpAIKitError):
    """Raised when host process fails to start"""

    def __init__(self, message: str = "Failed to start SharpAIKit host"):
        super().__init__(message, "HOST_STARTUP_ERROR")


class AgentNotFoundError(SharpAIKitError):
    """Raised when agent is not found"""

    def __init__(self, agent_id: str):
        super().__init__(f"Agent {agent_id} not found", "AGENT_NOT_FOUND")
        self.agent_id = agent_id


class ExecutionError(SharpAIKitError):
    """Raised when agent execution fails"""

    def __init__(self, message: str, error_code: str = None, denied_tools: list = None):
        super().__init__(message, error_code or "EXECUTION_ERROR")
        self.denied_tools = denied_tools or []


class SkillResolutionError(SharpAIKitError):
    """Raised when skill resolution fails"""

    def __init__(self, message: str):
        super().__init__(message, "SKILL_RESOLUTION_ERROR")

