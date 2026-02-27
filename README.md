# Task Status Transition Validation – Backend API

タスク管理における **状態遷移ルール（ToDo / Doing / Blocked / Done）を明確に定義・検証**することを目的とした  
バックエンド API サーバーです。  
フロントエンドと分離した **API 専用構成（ASP.NET Core + EF Core）** になっています。

---

## 🔗 公開URL（稼働確認）

- **API Root**  
  `GET /`  
  → API 案内を返却（Swagger / Health の案内）

- **Health Check**  
  `GET /health`  
  → `Healthy`

- **Swagger UI**  
  `GET /swagger`  
  → API 仕様確認・動作検証用

---

## 📌 できること（概要）

- JWT 認証によるユーザー認可
- プロジェクト管理
- タスク管理
- **タスク状態遷移の制御・検証**
  - 不正な遷移をサーバー側で防止
- ダッシュボード / レポート用 API

※ UI はフロントエンド側で実装しています（本リポジトリは API のみ）

---

## 🧱 アーキテクチャ概要

- ASP.NET Core Web API
- レイヤード構成
  - Controllers
  - Services（業務ロジック）
  - Repositories（EF Core）
  - Infrastructure
- 認証
  - JWT Bearer Authentication
- データアクセス
  - Entity Framework Core
  - SQL Server

---

## 🔐 認証方式

- JWT Bearer 認証
- Authorization Header にトークンを付与して API を呼び出します


---

## ⚙️ 環境変数 / 設定

### 必須設定（例）

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=...;User Id=...;Password=..."
  },
  "Jwt": {
    "Issuer": "TaskStatusTransitionValidation",
    "Audience": "TaskStatusTransitionValidation",
    "SigningKey": "your-secret-key"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://frontend-example.com"
    ]
  },
  "ENABLE_DB_MIGRATION": true,
  "ENABLE_SWAGGER": true
}
```
---

## 🗄️ DB / マイグレーション
- EF Core を使用
- 起動時に以下フラグが true の場合、自動で Database.Migrate() を実行

```json
ENABLE_DB_MIGRATION=true
```

---

## 🌱 デモデータ（Seeder）
- 初回起動時に デモ用データを自動投入
- Hosted Service にて実行
  
  ※ ローカル / Azure いずれでも動作

---

## 🚀 ローカル起動方法

```bash
dotnet restore
dotnet build
dotnet run
```
起動後：
- https://localhost:{port}/swagger
- https://localhost:{port}/health

---

## 🧪 テスト・検証
- Swagger UI から API 実行・レスポンス確認が可能
- JWT 認証付きエンドポイントも Swagger 上で動作確認可能

---

## 🔄 フロントエンドとの関係
- 本リポジトリ：バックエンド API
- フロントエンドは別リポジトリで管理
- CORS は AllowedOrigins にて制御

---

## 🧩 設計上のポイント
- 状態遷移ロジックをポリシーとして切り出し
- UI 側の実装ミスでも 不正な状態遷移を防止
- 業務ロジックは Service 層に集約
- API は UI 非依存で再利用可能

---

## 📄 ライセンス

Private / Portfolio Use