# 快速发布到 PyPI

## 前置条件

1. **PyPI 账号**: 在 https://pypi.org/account/register/ 注册
2. **API Token**: 在 https://pypi.org/manage/account/token/ 生成 API token
3. **安装工具**:
   ```bash
   pip install twine
   # 或
   uv pip install twine
   ```

## 快速步骤

### 1. 构建包

```bash
cd python-client

# 清理旧构建
rm -rf dist/ build/ *.egg-info/

# 生成 gRPC 代码
python3 generate_grpc.py

# 构建包
uv build
# 或
python3 -m build
```

### 2. 检查包

```bash
twine check dist/*
```

### 3. 上传到 PyPI

#### 方式 1: 使用 API Token（推荐）

```bash
twine upload dist/*
# Username: __token__
# Password: pypi-your-api-token-here
```

#### 方式 2: 使用配置文件

创建 `~/.pypirc`:

```ini
[pypi]
username = __token__
password = pypi-your-api-token-here
```

然后：

```bash
twine upload dist/*
```

### 4. 验证

访问 https://pypi.org/project/sharpaikit/ 查看你的包

安装测试：

```bash
pip install sharpaikit
```

## 使用自动化脚本

```bash
./publish_to_pypi.sh
```

脚本会自动：
1. 清理旧构建
2. 生成 gRPC 代码
3. 构建包
4. 检查包
5. 询问是否上传

## 注意事项

- 确保版本号已更新（`pyproject.toml` 和 `sharpaikit/__init__.py`）
- 确保 gRPC 代码已生成
- 首次发布建议先上传到 Test PyPI 测试

## Test PyPI 测试

```bash
# 上传到 Test PyPI
twine upload --repository testpypi dist/*

# 从 Test PyPI 安装测试
pip install --index-url https://test.pypi.org/simple/ sharpaikit
```

