"""
Process management for SharpAIKit gRPC host
"""

import subprocess
import time
import socket
import os
import platform
import signal
import atexit
from pathlib import Path
from typing import Optional
import logging

from .errors import HostStartupError, ConnectionError

logger = logging.getLogger(__name__)


class HostProcessManager:
    """Manages the lifecycle of the SharpAIKit gRPC host process"""

    def __init__(
        self,
        host: str = "localhost",
        port: int = 50051,
        host_executable: Optional[str] = None,
        host_working_dir: Optional[str] = None,
        startup_timeout: int = 30,
    ):
        """
        Initialize host process manager

        Args:
            host: Host address
            port: gRPC port
            host_executable: Path to host executable (auto-detected if None)
            host_working_dir: Working directory for host (auto-detected if None)
            startup_timeout: Timeout in seconds for host startup
        """
        self.host = host
        self.port = port
        self.startup_timeout = startup_timeout
        self._process: Optional[subprocess.Popen] = None
        self._host_executable = host_executable
        self._host_working_dir = host_working_dir

        # Register cleanup on exit
        atexit.register(self.cleanup)

    def _find_host_executable(self) -> Optional[str]:
        """Find the SharpAIKit.Grpc.Host executable"""
        # Try to find in common locations
        current_dir = Path(__file__).parent.parent.parent
        possible_paths = [
            current_dir / "src" / "SharpAIKit.Grpc.Host" / "bin" / "Release" / "net8.0" / "SharpAIKit.Grpc.Host",
            current_dir / "src" / "SharpAIKit.Grpc.Host" / "bin" / "Debug" / "net8.0" / "SharpAIKit.Grpc.Host",
            Path.home() / ".sharpaikit" / "host" / "SharpAIKit.Grpc.Host",
        ]

        # Add .exe extension on Windows
        if platform.system() == "Windows":
            possible_paths = [p.with_suffix(".exe") for p in possible_paths]

        for path in possible_paths:
            if path.exists() and path.is_file():
                return str(path.absolute())

        # Try dotnet run as fallback
        host_project = current_dir / "src" / "SharpAIKit.Grpc.Host" / "SharpAIKit.Grpc.Host.csproj"
        if host_project.exists():
            return None  # Will use dotnet run

        return None

    def _find_host_working_dir(self) -> Optional[str]:
        """Find the working directory for the host"""
        current_dir = Path(__file__).parent.parent.parent
        host_dir = current_dir / "src" / "SharpAIKit.Grpc.Host"
        if host_dir.exists():
            return str(host_dir.absolute())
        return None

    def is_running(self) -> bool:
        """Check if host is running by testing the port"""
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(1)
            result = sock.connect_ex((self.host, self.port))
            sock.close()
            return result == 0
        except Exception:
            return False

    def start(self) -> None:
        """Start the host process"""
        if self.is_running():
            logger.info(f"Host already running on {self.host}:{self.port}")
            return

        if self._process is not None:
            logger.warning("Host process already started")
            return

        executable = self._host_executable or self._find_host_executable()
        working_dir = self._host_working_dir or self._find_host_working_dir()

        env = os.environ.copy()
        env["SHARPAIKIT_GRPC_HOST"] = self.host
        env["SHARPAIKIT_GRPC_PORT"] = str(self.port)

        try:
            if executable:
                # Run compiled executable
                logger.info(f"Starting host: {executable}")
                self._process = subprocess.Popen(
                    [executable],
                    cwd=working_dir,
                    env=env,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    start_new_session=True,
                )
            else:
                # Use dotnet run
                if not working_dir:
                    raise HostStartupError("Cannot find host project directory")

                host_project = Path(working_dir) / "SharpAIKit.Grpc.Host.csproj"
                if not host_project.exists():
                    raise HostStartupError(f"Host project not found: {host_project}")

                logger.info(f"Starting host with dotnet run: {host_project}")
                self._process = subprocess.Popen(
                    ["dotnet", "run", "--project", str(host_project)],
                    cwd=working_dir,
                    env=env,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    start_new_session=True,
                )

            # Wait for host to be ready
            if not self._wait_for_ready():
                self._process.kill()
                stdout, stderr = self._process.communicate(timeout=5)
                error_msg = stderr.decode("utf-8", errors="ignore") if stderr else "Unknown error"
                raise HostStartupError(f"Host failed to start: {error_msg}")

            logger.info(f"Host started successfully on {self.host}:{self.port}")

        except Exception as e:
            if self._process:
                try:
                    self._process.kill()
                except Exception:
                    pass
                self._process = None
            raise HostStartupError(f"Failed to start host: {str(e)}") from e

    def _wait_for_ready(self) -> bool:
        """Wait for host to be ready"""
        start_time = time.time()
        while time.time() - start_time < self.startup_timeout:
            if self.is_running():
                return True
            if self._process and self._process.poll() is not None:
                # Process has exited
                return False
            time.sleep(0.5)
        return False

    def stop(self) -> None:
        """Stop the host process"""
        if self._process is None:
            return

        try:
            # Try graceful shutdown
            if platform.system() == "Windows":
                self._process.terminate()
            else:
                self._process.send_signal(signal.SIGTERM)

            # Wait for process to terminate
            try:
                self._process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                # Force kill if it doesn't terminate
                logger.warning("Host process did not terminate gracefully, forcing kill")
                self._process.kill()
                self._process.wait()

        except Exception as e:
            logger.warning(f"Error stopping host process: {e}")
            try:
                if self._process:
                    self._process.kill()
            except Exception:
                pass
        finally:
            self._process = None

    def cleanup(self) -> None:
        """Cleanup on exit"""
        self.stop()

    def __enter__(self):
        """Context manager entry"""
        self.start()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit"""
        self.cleanup()

