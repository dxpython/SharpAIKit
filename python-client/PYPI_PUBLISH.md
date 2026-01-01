# Publishing SharpAIKit Python SDK to PyPI

This guide explains how to build and publish the SharpAIKit Python SDK to PyPI.

## Prerequisites

1. **PyPI Account**: Create an account at https://pypi.org/account/register/
2. **API Token**: Generate an API token at https://pypi.org/manage/account/token/
3. **Build Tools**: Ensure you have `uv` or `build` installed:
   ```bash
   pip install build twine
   # or
   uv pip install build twine
   ```

## Step 1: Update Version

Before publishing, update the version in `pyproject.toml`:

```toml
[project]
version = "0.3.0"  # Update this
```

## Step 2: Prepare Package

### 2.1 Clean Previous Builds

```bash
cd python-client
rm -rf dist/ build/ *.egg-info/
```

### 2.2 Generate gRPC Code

The gRPC code must be generated before building:

```bash
python3 generate_grpc.py
```

### 2.3 Verify Package Structure

```bash
# Check what will be included
python3 -m build --sdist --wheel --outdir dist/
```

## Step 3: Build Package

### Using uv (Recommended)

```bash
uv build
```

This will create:
- `dist/sharpaikit-0.3.0-py3-none-any.whl` (wheel)
- `dist/sharpaikit-0.3.0.tar.gz` (source distribution)

### Using build

```bash
python3 -m build
```

## Step 4: Test Package Locally

Before publishing, test the package locally:

```bash
# Install from local build
uv pip install dist/sharpaikit-0.3.0-py3-none-any.whl

# Or
pip install dist/sharpaikit-0.3.0-py3-none-any.whl

# Test import
python3 -c "from sharpaikit import Agent; print('✅ Import successful')"
```

## Step 5: Check Package

Use `twine check` to verify the package:

```bash
twine check dist/*
```

This will check:
- Package metadata
- README format
- File structure

## Step 6: Upload to PyPI

### 6.1 Test PyPI (Recommended First)

Upload to Test PyPI first to verify everything works:

```bash
# Upload to Test PyPI
twine upload --repository testpypi dist/*

# Test installation from Test PyPI
pip install --index-url https://test.pypi.org/simple/ sharpaikit
```

### 6.2 Production PyPI

Once verified, upload to production PyPI:

```bash
# Upload to PyPI
twine upload dist/*
```

You'll be prompted for:
- Username: `__token__`
- Password: Your PyPI API token (starts with `pypi-`)

### 6.3 Using API Token (Recommended)

Create `~/.pypirc`:

```ini
[pypi]
username = __token__
password = pypi-your-api-token-here
```

Then upload:

```bash
twine upload dist/*
```

## Step 7: Verify Publication

After uploading, verify on PyPI:

1. Visit https://pypi.org/project/sharpaikit/
2. Check package metadata
3. Test installation:
   ```bash
   pip install sharpaikit
   ```

## Automated Script

Use the provided script:

```bash
./publish_to_pypi.sh
```

## Version Management

For subsequent releases:

1. Update version in `pyproject.toml`
2. Update `__version__` in `sharpaikit/__init__.py`
3. Update CHANGELOG.md (if exists)
4. Build and upload

## Troubleshooting

### Error: "File already exists"

The version already exists on PyPI. Update the version number.

### Error: "Invalid distribution"

Check that all required files are included in `MANIFEST.in`.

### Error: "README not found"

Ensure `README.md` exists and is specified in `pyproject.toml`.

### gRPC Code Missing

Make sure to run `generate_grpc.py` before building.

## Notes

- The package includes pre-generated gRPC code
- Users need .NET SDK to build the gRPC host (documented in README)
- The package is platform-independent (pure Python)

