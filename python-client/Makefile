.PHONY: generate-grpc install test clean

# Generate Python gRPC code from .proto file
generate-grpc:
	python generate_grpc.py

# Install the package
install:
	uv pip install -e .

# Run tests
test:
	pytest tests/

# Clean generated files
clean:
	rm -rf sharpaikit/_grpc/*_pb2.py
	rm -rf sharpaikit/_grpc/*_pb2_grpc.py
	rm -rf build/ dist/ *.egg-info/

