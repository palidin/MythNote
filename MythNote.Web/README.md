# MythNoteApi - ASP.NET Core 版本

这是 Myth Note API 的 ASP.NET Core 重构版本，提供笔记管理和文件上传功能。

## 项目结构

```
MythNoteApi/
├── Controllers/          # API 控制器
│   ├── AuthController.cs      # 认证控制器
│   ├── IndexController.cs     # 令牌管理控制器
│   ├── FileController.cs      # 文件/笔记管理控制器
│   └── UploadController.cs    # 文件上传控制器
├── DTOs/                 # 数据传输对象
├── Models/                # 数据模型
│   ├── AppDbContext.cs       # 数据库上下文
│   ├── User.cs             # 用户模型
│   ├── Note.cs             # 笔记模型
│   ├── Tag.cs              # 标签模型
│   └── NoteTag.cs          # 笔记-标签关联模型
├── Services/              # 业务逻辑服务
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── INoteService.cs
│   ├── NoteService.cs
│   ├── IUploadService.cs
│   └── UploadService.cs
└── Program.cs             # 应用程序入口
```

## 技术栈

- ASP.NET Core 10.0
- Entity Framework Core (SQLite)
- JWT Bearer 认证
- BCrypt.Net-Next 密码加密
- Swagger/OpenAPI

## API 端点

### 认证相关

| 方法 | 路径 | 描述 |
|------|--------|------|
| POST | `/auth/login` | 用户登录 |
| POST | `/auth/token/get` | 获取认证令牌 |
| POST | `/auth/token/destroy` | 销毁令牌 |
| POST | `/auth/token/check` | 检查令牌有效性 |
| GET | `/auth/token/user` | 获取当前用户信息 |

### 笔记管理

| 方法 | 路径 | 描述 |
|------|--------|------|
| POST | `/file/list` | 列出笔记文件 |
| POST | `/file/read` | 读取笔记内容 |
| POST | `/file/write` | 写入/保存笔记 |
| POST | `/file/delete` | 删除笔记 |
| POST | `/file/cleanup` | 清理文件系统 |
| GET | `/category/list` | 列出所有分类 |
| POST | `/category/rename` | 重命名分类 |
| POST | `/category/delete` | 删除分类 |
| POST | `/system/rebuild` | 重建系统索引 |
| GET | `/system/status` | 获取系统状态 |

### 文件上传

| 方法 | 路径 | 描述 |
|------|--------|------|
| POST | `/upload/image` | 上传图片文件 |
| POST | `/upload/image/base64` | 通过 Base64 上传图片 |
| POST | `/upload/image/url` | 通过 URL 上传图片 |

## 配置

在 `appsettings.json` 中配置以下内容：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=myth-note.db"
  },
  "Jwt": {
    "SecretKey": "your-secret-key",
    "Issuer": "MythNoteApi",
    "Audience": "MythNoteApiClient",
    "ExpiresInMinutes": "60"
  },
  "Note": {
    "Folder": "./notes",
    "ScanRule": "/**/*.md"
  },
  "Upload": {
    "Folder": "./uploads",
    "Host": "/uploads",
    "Extensions": {
      "Image": "jpg|jpeg|png|gif|webp"
    }
  }
}
```

## 运行项目

1. 还原依赖包：
```bash
dotnet restore
```

2. 构建项目：
```bash
dotnet build
```

3. 运行项目：
```bash
dotnet run
```

4. 访问 Swagger UI：
```
http://localhost:5000/swagger
```

## 数据库

项目使用 SQLite 数据库，首次运行时会自动创建数据库文件 `myth-note.db`。

数据库表结构：
- `user` - 用户表
- `note` - 笔记表
- `tag` - 标签表
- `note_tag` - 笔记-标签关联表

## 认证

使用 JWT Bearer Token 进行认证。登录后获取的 token 需要在请求头中携带：

```
Authorization: Bearer {your_token}
```

## 注意事项

- 首次运行前，需要手动创建默认用户（或通过数据库迁移脚本）
- 笔记文件存储在配置的 `Note:Folder` 目录中
- 上传的文件存储在配置的 `Upload:Folder` 目录中
