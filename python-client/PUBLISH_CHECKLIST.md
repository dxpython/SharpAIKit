# PyPI 发布检查清单

## ✅ 发布前检查

### 1. 版本号
- [ ] 更新 `pyproject.toml` 中的版本号
- [ ] 更新 `sharpaikit/__init__.py` 中的 `__version__`
- [ ] 确保版本号遵循语义化版本（如 0.3.0）

### 2. 元数据
- [ ] 检查 `pyproject.toml` 中的描述、作者、关键词
- [ ] 确保 README_PYPI.md 存在且内容完整
- [ ] 确保 LICENSE 文件存在

### 3. 代码
- [ ] 运行 `python3 generate_grpc.py` 生成最新的 gRPC 代码
- [ ] 确保所有 Python 文件没有语法错误
- [ ] 测试基本功能是否正常

### 4. 构建
- [ ] 清理旧构建：`rm -rf dist/ build/ *.egg-info/`
- [ ] 运行 `uv build` 或 `python3 -m build`
- [ ] 检查构建的文件：`ls -lh dist/`

### 5. 验证
- [ ] 运行 `twine check dist/*` 检查包
- [ ] 测试本地安装：`pip install dist/sharpaikit-0.3.0-py3-none-any.whl`
- [ ] 测试导入：`python3 -c "from sharpaikit import Agent"`

### 6. 文档
- [ ] README_PYPI.md 包含完整的使用说明
- [ ] 包含安装说明
- [ ] 包含快速开始示例
- [ ] 包含 API 参考链接

## 🚀 发布步骤

### 方式 1: 使用脚本（推荐）

```bash
./publish_to_pypi.sh
```

### 方式 2: 手动发布

```bash
# 1. 构建
uv build

# 2. 检查
twine check dist/*

# 3. 上传到 Test PyPI（可选，推荐首次发布）
twine upload --repository testpypi dist/*

# 4. 上传到 PyPI
twine upload dist/*
```

## 📝 发布后

- [ ] 访问 https://pypi.org/project/sharpaikit/ 验证
- [ ] 测试安装：`pip install sharpaikit`
- [ ] 更新 GitHub README 添加 PyPI 安装说明
- [ ] 创建 GitHub Release（可选）

## 🔑 PyPI 凭据

- **Username**: `__token__`
- **Password**: 你的 PyPI API token（以 `pypi-` 开头）

获取 API token: https://pypi.org/manage/account/token/

## ⚠️ 注意事项

1. **版本号不能重复**：如果版本已存在，需要更新版本号
2. **首次发布建议先测试**：使用 Test PyPI 测试
3. **gRPC 代码必须生成**：确保运行了 `generate_grpc.py`
4. **README 格式**：确保 README_PYPI.md 是有效的 Markdown

## 🐛 常见问题

### 错误: "File already exists"
- 解决：更新版本号

### 错误: "Invalid distribution"
- 解决：检查 MANIFEST.in 和 pyproject.toml 配置

### 错误: "README not found"
- 解决：确保 README_PYPI.md 存在且路径正确

### 错误: "gRPC code missing"
- 解决：运行 `python3 generate_grpc.py`

