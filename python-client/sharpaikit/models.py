"""
Data models for SharpAIKit Python SDK
"""

from dataclasses import dataclass, field
from typing import List, Dict, Optional, Any
from datetime import datetime


@dataclass
class ExecutionStep:
    """Represents a single step in agent execution"""

    step_number: int
    type: str
    thought: Optional[str] = None
    action: Optional[str] = None
    observation: Optional[str] = None
    tool_name: Optional[str] = None
    tool_args: Dict[str, str] = field(default_factory=dict)


@dataclass
class SkillConstraints:
    """Skill constraints information"""

    allowed_tools: List[str] = field(default_factory=list)
    forbidden_tools: List[str] = field(default_factory=list)
    max_steps: Optional[int] = None
    max_execution_time_ms: Optional[int] = None


@dataclass
class SkillResolution:
    """Skill resolution information"""

    activated_skill_ids: List[str] = field(default_factory=list)
    decision_reasons: List[str] = field(default_factory=list)
    constraints: Optional[SkillConstraints] = None
    tool_denial_reasons: Dict[str, str] = field(default_factory=dict)


@dataclass
class ExecutionResult:
    """Result of agent execution"""

    output: str
    success: bool
    steps: List[ExecutionStep] = field(default_factory=list)
    skill_resolution: Optional[SkillResolution] = None
    denied_tools: List[str] = field(default_factory=list)
    error: Optional[str] = None
    error_code: Optional[str] = None

    @property
    def skill_trace(self) -> List[str]:
        """Get skill trace as a list of skill IDs"""
        if self.skill_resolution:
            return self.skill_resolution.activated_skill_ids
        return []


@dataclass
class SkillInfo:
    """Information about a skill"""

    id: str
    name: str
    description: str
    version: str = ""
    priority: int = 0
    scope: str = ""


@dataclass
class ChainContext:
    """Chain execution context"""
    input: str = ""
    output: str = ""
    data: Dict[str, str] = field(default_factory=dict)
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "input": self.input,
            "output": self.output,
            "data": self.data
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "ChainContext":
        return cls(
            input=data.get("input", ""),
            output=data.get("output", ""),
            data=data.get("data", {})
        )


@dataclass
class ChatMessage:
    """Chat message"""
    role: str
    content: str
    name: Optional[str] = None


@dataclass
class SearchResult:
    """Search result from RAG"""
    document: Dict[str, Any] = field(default_factory=dict)
    similarity: float = 0.0


@dataclass
class GraphState:
    """Graph execution state"""
    current_node: str = ""
    next_node: str = ""
    should_end: bool = False
    output: str = ""
    data: Dict[str, str] = field(default_factory=dict)
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "current_node": self.current_node,
            "next_node": self.next_node,
            "should_end": self.should_end,
            "output": self.output,
            "data": self.data
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "GraphState":
        return cls(
            current_node=data.get("current_node", ""),
            next_node=data.get("next_node", ""),
            should_end=data.get("should_end", False),
            output=data.get("output", ""),
            data=data.get("data", {})
        )


@dataclass
class Document:
    """Document loaded from various sources"""
    content: str
    metadata: Dict[str, str] = field(default_factory=dict)
    source: Optional[str] = None


@dataclass
class CodeExecutionResult:
    """Result of code execution"""
    success: bool
    output: str = ""
    error: Optional[str] = None
    execution_time_ms: Optional[int] = None


@dataclass
class OptimizationResult:
    """Result of prompt optimization"""
    success: bool
    optimized_prompt: str = ""
    metric_score: float = 0.0
    steps: List[Dict[str, Any]] = field(default_factory=list)
    error: Optional[str] = None

