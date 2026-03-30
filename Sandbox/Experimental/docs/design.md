# DeviceManager 設計書

## 1. システム概要

Blazor Server ベースのモバイルデバイス管理アプリケーション。
サーバ側で端末の状態管理・コンフィグ配信・メッセージングを行い、端末側は MAUI SDK を通じてサーバと通信する。

### アーキテクチャ概要

```
┌─────────────────────────────────────────────────┐
│           Blazor Server (ASP.NET Core)          │
│  ┌───────────┐ ┌──────────┐ ┌────────────────┐ │
│  │ Dashboard  │ │ REST API │ │  SignalR Hub   │ │
│  │ (Blazor)   │ │          │ │                │ │
│  └───────────┘ └──────────┘ └────────────────┘ │
│  ┌───────────────────────────────────────────┐  │
│  │          Service Layer                     │  │
│  │  DeviceService / ConfigService /           │  │
│  │  StorageService / MessageService           │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │          Data Layer (SQLite)               │  │
│  │  共通DB (Common.db) + 端末別DB (Device_*.db)│  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
              │ SignalR WebSocket │ REST API
              ▼                  ▼
┌─────────────────────────────────────────────────┐
│           MAUI アプリ (Android / iOS)            │
│  ┌───────────────────────────────────────────┐  │
│  │      DeviceManager.Client.Sdk             │  │
│  │  ┌─────────────┐ ┌──────────────────┐    │  │
│  │  │ SignalR通信   │ │  REST API通信     │    │  │
│  │  └─────────────┘ └──────────────────┘    │  │
│  │  ┌─────────────┐ ┌──────────────────┐    │  │
│  │  │ コールバック   │ │ プロバイダー       │    │  │
│  │  │ (イベント)    │ │ (デバイス情報)     │    │  │
│  │  └─────────────┘ └──────────────────┘    │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │    プラットフォーム固有実装 (Android/iOS)     │  │
│  │    ※ SDK外。利用者が実装する                  │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

### 技術スタック

| レイヤー | 技術 |
|---------|------|
| サーバ UI | Blazor Server (カスタムCSS、MudBlazor不使用) |
| サーバ API | ASP.NET Core Web API + Swagger/OpenAPI |
| リアルタイム通信 (ダッシュボード) | SignalR |
| デバイス通信 | gRPC (Protocol Buffers) + SignalR (フォールバック) |
| データベース | SQLite (Microsoft.Data.Sqlite) |
| データアクセス | 直接SQL (Dapper不使用、Microsoft.Data.Sqlite直接) |
| ログ | Serilog |
| クライアント SDK | .NET 10 Class Library |
| クライアントアプリ | .NET MAUI / WPF (テストクライアント) |
| API ドキュメント | Swashbuckle (Swagger UI: /swagger) |

---

## 2. 機能一覧

### 2.1 サーバ機能

| # | カテゴリ | 機能名 | 説明 |
|---|---------|--------|------|
| S-01 | ダッシュボード | 端末ステータスサマリー | Active / Success / Warning / Error の端末数を集計表示 |
| S-02 | ダッシュボード | 端末一覧表示 | 全端末のリスト表示（名前、レベル、プログレス、最終通信日時、ステータス） |
| S-03 | ダッシュボード | 端末詳細表示 | 個別端末の詳細情報（位置、バッテリー、接続履歴、ストレージ使用量等） |
| S-04 | ダッシュボード | リアルタイム更新 | SignalR による端末ステータスのリアルタイム反映 |
| S-05 | ダッシュボード | フィルタ・検索 | ステータス、名前、グループによる端末の絞り込み |
| S-06 | メッセージング | 端末へのメッセージ送信 | サーバから個別端末/全端末へのメッセージ送信 |
| S-07 | メッセージング | 端末からのメッセージ受信 | 端末から送信されたメッセージの受信・表示 |
| S-08 | メッセージング | メッセージ履歴 | 送受信メッセージの履歴閲覧 |
| S-09 | メッセージング | コマンド実行 | 端末へのリモートコマンド送信（再起動要求、設定反映要求等） |
| S-10 | データストア | キー・バリュー登録 | 文字列キー・バリューの CRUD 操作（API） |
| S-11 | データストア | 端末別データストア | 端末ごとに独立したキー・バリュー領域 |
| S-12 | データストア | 共通データストア | 全端末共有のキー・バリュー領域 |
| S-13 | データストア | データストア閲覧 | Web UI からのデータ閲覧・編集 |
| S-14 | コンフィグ | 共通コンフィグ管理 | 全端末共通の設定値を管理 |
| S-15 | コンフィグ | 端末別コンフィグ管理 | 端末ごとの設定値を管理（共通値のオーバーライド可能） |
| S-16 | コンフィグ | コンフィグ配信 | コンフィグ変更時の端末への自動配信（SignalR） |
| S-17 | コンフィグ | コンフィグ履歴 | コンフィグ変更履歴の記録 |
| S-18 | 端末管理 | 端末登録 | 新規端末の登録（手動 / 自動登録） |
| S-19 | 端末管理 | 端末グループ管理 | 端末のグループ分け（タグベース） |
| S-20 | 端末管理 | 端末削除・無効化 | 端末の論理削除・無効化 |
| S-21 | ファイルストレージ | ファイルアップロード | 端末からのファイルアップロード受付 |
| S-22 | ファイルストレージ | ファイルダウンロード | 端末へのファイル配信 |
| S-23 | システム | ヘルスチェック | サーバ死活監視エンドポイント |
| S-24 | システム | ログ閲覧 | アプリケーションログの Web UI 閲覧 |
| S-25 | システム | メトリクス | Prometheus 形式のメトリクス出力 |

### 2.2 MAUI SDK 機能

| # | カテゴリ | 機能名 | 説明 |
|---|---------|--------|------|
| C-01 | 接続管理 | サーバ接続 | SignalR によるサーバへの接続・自動再接続 |
| C-02 | 接続管理 | 接続状態監視 | 接続状態の変化通知（コールバック） |
| C-03 | 端末登録 | 自動登録 | 初回接続時のサーバへの端末自動登録 |
| C-04 | 端末登録 | 端末情報プロバイダー | デバイス情報取得のインターフェース（利用者が実装） |
| C-05 | ステータス | ステータス送信 | 端末ステータスの定期送信 |
| C-06 | ステータス | ステータスプロバイダー | ステータス情報取得のインターフェース（利用者が実装） |
| C-07 | メッセージング | メッセージ送信 | サーバへのメッセージ送信 |
| C-08 | メッセージング | メッセージ受信 | サーバからのメッセージ受信（コールバック） |
| C-09 | メッセージング | コマンド受信 | サーバからのコマンド受信・実行（コールバック） |
| C-10 | データストア | KV データ読み書き | サーバのキー・バリューストアへの CRUD |
| C-11 | コンフィグ | コンフィグ取得 | サーバから端末向けコンフィグを取得 |
| C-12 | コンフィグ | コンフィグ変更通知 | コンフィグ変更時の通知受信（コールバック） |
| C-13 | コンフィグ | ローカルキャッシュ | コンフィグのローカルキャッシュ（オフライン対応） |
| C-14 | ファイル | ファイルアップロード | サーバへのファイルアップロード（REST API） |
| C-15 | ファイル | ファイルダウンロード | サーバからのファイルダウンロード（REST API） |

---

## 3. 画面一覧

### 3.1 画面遷移図

```
サイドバー
├── ダッシュボード (/)
│   └── 端末詳細ダイアログ
├── 端末管理 (/devices)
│   ├── 端末登録ダイアログ
│   └── 端末編集ダイアログ
├── メッセージ (/messages)
│   ├── メッセージ送信ダイアログ
│   └── メッセージ履歴
├── データストア (/datastore)
│   ├── 共通ストア
│   └── 端末別ストア
├── コンフィグ (/config)
│   ├── 共通コンフィグ
│   ├── 端末別コンフィグ
│   └── コンフィグ編集ダイアログ
├── ファイル (/files)
│   └── ファイル一覧 / アップロード
└── 設定 (/settings)
    └── システム設定
```

### 3.2 画面詳細

| # | 画面名 | パス | 説明 |
|---|--------|------|------|
| P-01 | ダッシュボード | `/` | ステータスサマリーカード (Active/Success/Warning/Error) + 端末一覧テーブル |
| P-02 | 端末詳細 | ダイアログ | 端末の詳細情報、接続履歴、ステータス推移グラフ |
| P-03 | 端末管理 | `/devices` | 端末の一覧・登録・編集・削除。グループ管理 |
| P-04 | 端末登録/編集 | ダイアログ | 端末名、グループ、タグ、メモの入力フォーム |
| P-05 | メッセージ | `/messages` | メッセージ送受信画面。対象端末選択+メッセージ入力+履歴表示 |
| P-06 | メッセージ送信 | ダイアログ | 送信先（個別/グループ/全体）、メッセージ種別、本文入力 |
| P-07 | データストア | `/datastore` | キー・バリューの閲覧。共通/端末別の切り替えタブ |
| P-08 | データストア編集 | ダイアログ | キー・バリューの追加・編集フォーム |
| P-09 | コンフィグ管理 | `/config` | 共通コンフィグと端末別コンフィグの管理。ツリー表示 |
| P-10 | コンフィグ編集 | ダイアログ | コンフィグ値の編集。型（文字列/数値/真偽値/JSON）選択 |
| P-11 | ファイル管理 | `/files` | ファイルストレージのブラウズ。端末別フォルダ構成 |
| P-12 | システム設定 | `/settings` | システム全体の設定（ログレベル、通知設定等） |
| P-13 | ログビューア | `/logs` | アプリケーションログのリアルタイム閲覧 |

### 3.3 ダッシュボード画面レイアウト

```
┌──────────────────────────────────────────────────────────────┐
│ [≡] DeviceManager                              [🔔] [👤] [⚙] │
├────────┬─────────────────────────────────────────────────────┤
│        │  ● Active    ✓ Success    ⚠ Warning    ✕ Error     │
│  Nav   │ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐       │
│        │ │  100   │ │  100   │ │  100   │ │  100   │       │
│ ・Dash │ └────────┘ └────────┘ └────────┘ └────────┘       │
│ ・端末  │                                                     │
│ ・MSG  │ ┌─────┬────────┬───────┬──────────┬────────┬─────┐ │
│ ・Data │ │ ○   │ Name   │ Level │ Progress │ Time   │ Act │ │
│ ・Config│ ├─────┼────────┼───────┼──────────┼────────┼─────┤ │
│ ・Files │ │ 🟢  │Dev001  │ ██   │ ████░░░  │ 15:30  │ [>] │ │
│ ・設定  │ │ 🟡  │Dev002  │ █    │ ██░░░░░  │ 15:28  │ [>] │ │
│        │ │ 🔴  │Dev003  │ ███  │ █████░░  │ 15:25  │ [>] │ │
│        │ │ ...  │        │      │          │        │     │ │
│        │ └─────┴────────┴───────┴──────────┴────────┴─────┘ │
│        │ [< 1 2 3 ... >]                                     │
└────────┴─────────────────────────────────────────────────────┘
```

---

## 4. データベース設計

### 4.1 DB 分離方針

| DB ファイル | 用途 | 説明 |
|------------|------|------|
| `Common.db` | 共通データ | 端末マスタ、共通コンフィグ、共通データストア、メッセージ履歴 |
| `Device_{DeviceId}.db` | 端末別データ | 端末固有の KV データ、端末別コンフィグ、ステータス履歴 |

端末別 DB を分離することで：
- 端末数増加時のスケーラビリティ確保
- 端末削除時のデータクリーンアップが容易
- 端末間のデータ分離が明確

### 4.2 共通 DB (Common.db)

#### Device（端末マスタ）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| DeviceId | TEXT | PK | 端末一意識別子 (例: UUID) |
| Name | TEXT | NOT NULL | 端末表示名 |
| Group | TEXT | | グループ名 |
| Tags | TEXT | | タグ (JSON配列) |
| Status | INTEGER | NOT NULL, DEFAULT 0 | 0:Inactive, 1:Active, 2:Warning, 3:Error |
| IsEnabled | INTEGER | NOT NULL, DEFAULT 1 | 有効/無効フラグ |
| Note | TEXT | | メモ |
| RegisteredAt | TEXT | NOT NULL | 登録日時 (ISO 8601) |
| LastConnectedAt | TEXT | | 最終接続日時 |
| CreatedAt | TEXT | NOT NULL | 作成日時 |
| UpdatedAt | TEXT | NOT NULL | 更新日時 |

```sql
CREATE TABLE Device (
    DeviceId       TEXT    PRIMARY KEY,
    Name           TEXT    NOT NULL,
    "Group"        TEXT,
    Tags           TEXT,
    Status         INTEGER NOT NULL DEFAULT 0,
    IsEnabled      INTEGER NOT NULL DEFAULT 1,
    Note           TEXT,
    RegisteredAt   TEXT    NOT NULL,
    LastConnectedAt TEXT,
    CreatedAt      TEXT    NOT NULL,
    UpdatedAt      TEXT    NOT NULL
);

CREATE INDEX IX_Device_Status ON Device(Status);
CREATE INDEX IX_Device_Group ON Device("Group");
```

#### DeviceStatus（端末ステータススナップショット - 最新のみ共通DBに保持）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| DeviceId | TEXT | PK, FK | 端末ID |
| Level | INTEGER | NOT NULL | レベル値 (0-100) |
| Progress | REAL | NOT NULL | 進捗 (0.0-1.0) |
| Battery | INTEGER | | バッテリー残量 (0-100) |
| Latitude | REAL | | 緯度 |
| Longitude | REAL | | 経度 |
| CustomData | TEXT | | カスタムデータ (JSON) |
| Timestamp | TEXT | NOT NULL | 報告日時 |

```sql
CREATE TABLE DeviceStatus (
    DeviceId    TEXT    PRIMARY KEY REFERENCES Device(DeviceId),
    Level       INTEGER NOT NULL DEFAULT 0,
    Progress    REAL    NOT NULL DEFAULT 0,
    Battery     INTEGER,
    Latitude    REAL,
    Longitude   REAL,
    CustomData  TEXT,
    Timestamp   TEXT    NOT NULL
);
```

#### CommonConfig（共通コンフィグ）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Key | TEXT | PK | コンフィグキー (ドット区切り階層: "app.theme.color") |
| Value | TEXT | NOT NULL | コンフィグ値 |
| ValueType | TEXT | NOT NULL, DEFAULT 'string' | 型 (string/number/boolean/json) |
| Description | TEXT | | 説明 |
| UpdatedAt | TEXT | NOT NULL | 更新日時 |

```sql
CREATE TABLE CommonConfig (
    Key         TEXT PRIMARY KEY,
    Value       TEXT NOT NULL,
    ValueType   TEXT NOT NULL DEFAULT 'string',
    Description TEXT,
    UpdatedAt   TEXT NOT NULL
);
```

#### CommonDataStore（共通キー・バリューストア）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Key | TEXT | PK | データキー |
| Value | TEXT | NOT NULL | データ値 |
| CreatedAt | TEXT | NOT NULL | 作成日時 |
| UpdatedAt | TEXT | NOT NULL | 更新日時 |

```sql
CREATE TABLE CommonDataStore (
    Key       TEXT PRIMARY KEY,
    Value     TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

#### Message（メッセージ履歴）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| MessageId | INTEGER | PK, AUTOINCREMENT | メッセージID |
| DeviceId | TEXT | FK | 対象端末 (NULLの場合は全端末向け) |
| Direction | INTEGER | NOT NULL | 0:ServerToDevice, 1:DeviceToServer |
| MessageType | TEXT | NOT NULL | メッセージ種別 (text/command/notification) |
| Content | TEXT | NOT NULL | メッセージ内容 |
| Status | INTEGER | NOT NULL, DEFAULT 0 | 0:Sent, 1:Delivered, 2:Read, 3:Failed |
| CreatedAt | TEXT | NOT NULL | 送信日時 |

```sql
CREATE TABLE Message (
    MessageId   INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceId    TEXT    REFERENCES Device(DeviceId),
    Direction   INTEGER NOT NULL,
    MessageType TEXT    NOT NULL,
    Content     TEXT    NOT NULL,
    Status      INTEGER NOT NULL DEFAULT 0,
    CreatedAt   TEXT    NOT NULL
);

CREATE INDEX IX_Message_DeviceId ON Message(DeviceId);
CREATE INDEX IX_Message_CreatedAt ON Message(CreatedAt);
```

#### ConfigHistory（コンフィグ変更履歴）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Id | INTEGER | PK, AUTOINCREMENT | 履歴ID |
| Scope | TEXT | NOT NULL | 'common' or DeviceId |
| Key | TEXT | NOT NULL | コンフィグキー |
| OldValue | TEXT | | 変更前の値 |
| NewValue | TEXT | | 変更後の値 |
| ChangedAt | TEXT | NOT NULL | 変更日時 |

```sql
CREATE TABLE ConfigHistory (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Scope     TEXT NOT NULL,
    Key       TEXT NOT NULL,
    OldValue  TEXT,
    NewValue  TEXT,
    ChangedAt TEXT NOT NULL
);

CREATE INDEX IX_ConfigHistory_Scope_Key ON ConfigHistory(Scope, Key);
```

### 4.3 端末別 DB (Device_{DeviceId}.db)

#### DeviceConfig（端末別コンフィグ）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Key | TEXT | PK | コンフィグキー |
| Value | TEXT | NOT NULL | コンフィグ値（共通コンフィグのオーバーライド） |
| ValueType | TEXT | NOT NULL, DEFAULT 'string' | 型 |
| UpdatedAt | TEXT | NOT NULL | 更新日時 |

```sql
CREATE TABLE DeviceConfig (
    Key       TEXT PRIMARY KEY,
    Value     TEXT NOT NULL,
    ValueType TEXT NOT NULL DEFAULT 'string',
    UpdatedAt TEXT NOT NULL
);
```

#### DeviceDataStore（端末別キー・バリューストア）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Key | TEXT | PK | データキー |
| Value | TEXT | NOT NULL | データ値 |
| CreatedAt | TEXT | NOT NULL | 作成日時 |
| UpdatedAt | TEXT | NOT NULL | 更新日時 |

```sql
CREATE TABLE DeviceDataStore (
    Key       TEXT PRIMARY KEY,
    Value     TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

#### StatusHistory（ステータス履歴）

| カラム | 型 | 制約 | 説明 |
|--------|------|------|------|
| Id | INTEGER | PK, AUTOINCREMENT | 履歴ID |
| Level | INTEGER | NOT NULL | レベル値 |
| Progress | REAL | NOT NULL | 進捗 |
| Battery | INTEGER | | バッテリー |
| Latitude | REAL | | 緯度 |
| Longitude | REAL | | 経度 |
| CustomData | TEXT | | カスタムデータ |
| Timestamp | TEXT | NOT NULL | 報告日時 |

```sql
CREATE TABLE StatusHistory (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Level      INTEGER NOT NULL DEFAULT 0,
    Progress   REAL    NOT NULL DEFAULT 0,
    Battery    INTEGER,
    Latitude   REAL,
    Longitude  REAL,
    CustomData TEXT,
    Timestamp  TEXT    NOT NULL
);

CREATE INDEX IX_StatusHistory_Timestamp ON StatusHistory(Timestamp);
```

---

## 5. API 設計

### 5.1 REST API

#### サーバ情報

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/server/time` | サーバ時刻取得 |
| GET | `/api/server/health` | ヘルスチェック |

#### 端末管理

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/devices` | 端末一覧取得 |
| GET | `/api/devices/{deviceId}` | 端末詳細取得 |
| POST | `/api/devices` | 端末登録 |
| PUT | `/api/devices/{deviceId}` | 端末情報更新 |
| DELETE | `/api/devices/{deviceId}` | 端末削除 |
| POST | `/api/devices/{deviceId}/status` | ステータス報告（端末→サーバ） |
| GET | `/api/devices/{deviceId}/status/history` | ステータス履歴取得 |

#### データストア

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/datastore/common` | 共通KV一覧取得 |
| GET | `/api/datastore/common/{key}` | 共通KV取得 |
| PUT | `/api/datastore/common/{key}` | 共通KV登録/更新 |
| DELETE | `/api/datastore/common/{key}` | 共通KV削除 |
| GET | `/api/datastore/devices/{deviceId}` | 端末別KV一覧取得 |
| GET | `/api/datastore/devices/{deviceId}/{key}` | 端末別KV取得 |
| PUT | `/api/datastore/devices/{deviceId}/{key}` | 端末別KV登録/更新 |
| DELETE | `/api/datastore/devices/{deviceId}/{key}` | 端末別KV削除 |

#### コンフィグ

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/config/common` | 共通コンフィグ一覧取得 |
| PUT | `/api/config/common/{key}` | 共通コンフィグ登録/更新 |
| DELETE | `/api/config/common/{key}` | 共通コンフィグ削除 |
| GET | `/api/config/devices/{deviceId}` | 端末別コンフィグ一覧取得（共通+端末マージ） |
| PUT | `/api/config/devices/{deviceId}/{key}` | 端末別コンフィグ登録/更新 |
| DELETE | `/api/config/devices/{deviceId}/{key}` | 端末別コンフィグ削除 |
| GET | `/api/config/devices/{deviceId}/resolved` | 端末の最終コンフィグ取得（共通+端末マージ済み） |

#### ファイルストレージ

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/storage/{**path}` | ファイル/ディレクトリ取得 |
| POST | `/api/storage/{**path}` | ファイルアップロード |
| DELETE | `/api/storage/{**path}` | ファイル削除 |

#### メッセージ

| メソッド | パス | 説明 |
|---------|------|------|
| GET | `/api/messages` | メッセージ履歴取得 |
| GET | `/api/messages/devices/{deviceId}` | 端末別メッセージ履歴取得 |

### 5.2 SignalR Hub

#### DeviceHub (`/hubs/device`)

**サーバ → クライアント (端末) メソッド:**

| メソッド名 | パラメータ | 説明 |
|-----------|------------|------|
| `ReceiveMessage` | `MessageType type, string content` | メッセージ受信 |
| `ReceiveCommand` | `string command, string? payload` | コマンド受信 |
| `ConfigUpdated` | `string key, string value` | コンフィグ変更通知 |
| `ConfigReload` | - | コンフィグ全体リロード指示 |

**クライアント (端末) → サーバ メソッド:**

| メソッド名 | パラメータ | 説明 |
|-----------|------------|------|
| `Register` | `DeviceRegistration info` | 端末登録/接続 |
| `ReportStatus` | `DeviceStatusReport report` | ステータス報告 |
| `SendMessage` | `string messageType, string content` | メッセージ送信 |
| `CommandResult` | `string commandId, bool success, string? result` | コマンド実行結果報告 |

**サーバ → Blazor UI メソッド (別コンテキスト):**

| メソッド名 | パラメータ | 説明 |
|-----------|------------|------|
| `DeviceConnected` | `string deviceId` | 端末接続通知 |
| `DeviceDisconnected` | `string deviceId` | 端末切断通知 |
| `DeviceStatusUpdated` | `string deviceId, DeviceStatusReport report` | ステータス更新通知 |
| `MessageReceived` | `string deviceId, string type, string content` | メッセージ受信通知 |

---

## 6. SDK 設計 (DeviceManager.Client.Sdk)

### 6.1 プロジェクト構成

```
DeviceManager.Client.Sdk/
├── DeviceManagerClient.cs          # メインクライアントクラス
├── DeviceManagerClientOptions.cs   # 接続設定
├── IDeviceInfoProvider.cs          # デバイス情報プロバイダー(IF)
├── IDeviceStatusProvider.cs        # ステータスプロバイダー(IF)
├── IDeviceCommandHandler.cs        # コマンドハンドラー(IF)
├── Connection/
│   ├── IConnectionManager.cs       # 接続管理インターフェース
│   ├── SignalRConnectionManager.cs  # SignalR接続管理実装
│   └── ConnectionState.cs          # 接続状態列挙
├── Config/
│   ├── IConfigManager.cs           # コンフィグ管理インターフェース
│   ├── ConfigManager.cs            # コンフィグ管理実装
│   └── ConfigCache.cs              # ローカルキャッシュ
├── DataStore/
│   ├── IDataStoreClient.cs         # データストアクライアントIF
│   └── DataStoreClient.cs          # データストアクライアント実装
├── Messaging/
│   ├── IMessageClient.cs           # メッセージクライアントIF
│   └── MessageClient.cs            # メッセージクライアント実装
├── Storage/
│   ├── IStorageClient.cs           # ファイルストレージクライアントIF
│   └── StorageClient.cs            # ファイルストレージクライアント実装
└── Models/
    ├── DeviceRegistration.cs       # 端末登録情報
    ├── DeviceStatusReport.cs       # ステータス報告
    ├── ConfigEntry.cs              # コンフィグエントリ
    ├── DataStoreEntry.cs           # KVエントリ
    └── ServerMessage.cs            # サーバメッセージ
```

### 6.2 主要インターフェース

#### IDeviceInfoProvider（利用者が実装）

```csharp
/// <summary>
/// デバイス固有情報の取得プロバイダー。
/// Android/iOS 固有の実装は利用者が行う。
/// </summary>
public interface IDeviceInfoProvider
{
    /// <summary>端末の一意識別子</summary>
    string DeviceId { get; }

    /// <summary>端末の表示名</summary>
    string DeviceName { get; }

    /// <summary>プラットフォーム情報 (例: "Android 14", "iOS 17.4")</summary>
    string Platform { get; }

    /// <summary>追加のデバイス情報 (任意)</summary>
    IDictionary<string, string>? AdditionalInfo { get; }
}
```

#### IDeviceStatusProvider（利用者が実装）

```csharp
/// <summary>
/// デバイスステータスの取得プロバイダー。
/// バッテリー残量、位置情報等、プラットフォーム固有データの取得は利用者が実装する。
/// </summary>
public interface IDeviceStatusProvider
{
    /// <summary>現在のステータス情報を取得</summary>
    ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
```

#### IDeviceCommandHandler（利用者が実装）

```csharp
/// <summary>
/// サーバからのコマンドを処理するハンドラー。
/// コマンドの解釈と実行は利用者が実装する。
/// </summary>
public interface IDeviceCommandHandler
{
    /// <summary>コマンドを実行し、結果を返す</summary>
    ValueTask<CommandResult> HandleCommandAsync(string command, string? payload, CancellationToken cancellationToken = default);
}
```

#### DeviceManagerClient（SDKメインクラス）

```csharp
public sealed class DeviceManagerClient : IAsyncDisposable
{
    // 初期化・接続
    public DeviceManagerClient(DeviceManagerClientOptions options,
        IDeviceInfoProvider deviceInfoProvider,
        IDeviceStatusProvider? statusProvider = null,
        IDeviceCommandHandler? commandHandler = null);

    public Task ConnectAsync(CancellationToken cancellationToken = default);
    public Task DisconnectAsync();

    // 接続状態
    public ConnectionState State { get; }
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    // メッセージング
    public IMessageClient Messages { get; }

    // データストア
    public IDataStoreClient DataStore { get; }

    // コンフィグ
    public IConfigManager Config { get; }

    // ファイルストレージ
    public IStorageClient Storage { get; }

    // ステータス自動送信の開始・停止
    public Task StartStatusReportingAsync(TimeSpan interval);
    public Task StopStatusReportingAsync();
}
```

#### DeviceManagerClientOptions

```csharp
public sealed class DeviceManagerClientOptions
{
    /// <summary>サーバのベース URL</summary>
    public required string ServerUrl { get; init; }

    /// <summary>自動再接続の有効/無効</summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>再接続の最大リトライ間隔</summary>
    public TimeSpan MaxReconnectInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>ステータス送信間隔のデフォルト値</summary>
    public TimeSpan DefaultStatusInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>APIタイムアウト</summary>
    public TimeSpan ApiTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>コンフィグのローカルキャッシュパス (null でキャッシュ無効)</summary>
    public string? ConfigCachePath { get; init; }
}
```

### 6.3 SDK の DI 登録パターン

```csharp
// MAUI アプリでの登録例
builder.Services.AddDeviceManagerClient(options =>
{
    options.ServerUrl = "https://dm.example.com";
    options.AutoReconnect = true;
    options.ConfigCachePath = FileSystem.AppDataDirectory;
});

// プロバイダーの登録（利用者が実装したクラス）
builder.Services.AddSingleton<IDeviceInfoProvider, AndroidDeviceInfoProvider>();
builder.Services.AddSingleton<IDeviceStatusProvider, AndroidStatusProvider>();
builder.Services.AddSingleton<IDeviceCommandHandler, MyCommandHandler>();
```

### 6.4 コールバック・イベント一覧

| イベント/コールバック | 発生タイミング | 用途 |
|---------------------|---------------|------|
| `ConnectionStateChanged` | 接続状態変更時 | UI更新、再接続ロジック |
| `IMessageClient.MessageReceived` | サーバからメッセージ受信 | メッセージ表示、処理 |
| `IConfigManager.ConfigChanged` | コンフィグ変更通知受信 | 設定値の反映 |
| `IDeviceCommandHandler.HandleCommandAsync` | コマンド受信時 (Provider) | コマンド処理の実装 |

---

## 7. プロジェクト構成

```
DeviceManager.sln
├── src/
│   ├── DeviceManager.Server.Core/        # サーバ ビジネスロジック・データアクセス
│   │   ├── Models/                       # エンティティ、DTO
│   │   ├── Services/                     # ビジネスサービス
│   │   ├── DataAccess/                   # Data Accessor インターフェース
│   │   └── DeviceManager.Server.Core.csproj
│   │
│   ├── DeviceManager.Server.Web/         # Blazor Server Web アプリ
│   │   ├── Components/                   # 共通コンポーネント
│   │   ├── Pages/                        # 画面 (Razor Pages)
│   │   ├── Hubs/                         # SignalR ハブ
│   │   ├── Controllers/                  # REST API コントローラー
│   │   ├── wwwroot/                      # 静的ファイル
│   │   └── DeviceManager.Server.Web.csproj
│   │
│   ├── DeviceManager.Shared/             # サーバ・クライアント共有モデル
│   │   ├── Models/                       # 共有DTO
│   │   ├── Constants/                    # 定数
│   │   └── DeviceManager.Shared.csproj
│   │
│   └── DeviceManager.Client.Sdk/         # MAUI 用 SDK
│       ├── (上記 6.1 の構成)
│       └── DeviceManager.Client.Sdk.csproj
│
└── tests/
    ├── DeviceManager.Server.Core.Tests/
    └── DeviceManager.Client.Sdk.Tests/
```

---

## 8. コンフィグ解決ルール

端末が受け取るコンフィグは以下の優先順位で解決される：

```
端末別コンフィグ (DeviceConfig)  ← 最優先
    ↓ fallback
共通コンフィグ (CommonConfig)    ← デフォルト値
```

- 同一キーが両方に存在する場合、端末別コンフィグが優先される
- 端末別コンフィグに存在しないキーは、共通コンフィグの値が使用される
- `/api/config/devices/{deviceId}/resolved` API でマージ済みコンフィグを取得可能

---

## 9. SignalR 接続管理

### 9.1 端末接続フロー

```
端末                           サーバ
 │                              │
 │──── SignalR Connect ────────>│
 │                              │
 │──── Register(DeviceInfo) ───>│ ← 端末情報を DB に保存/更新
 │                              │   ConnectionId と DeviceId を紐付け
 │<─── ConfigReload ───────────│ ← 最新コンフィグを送信
 │                              │
 │──── ReportStatus(status) ──>│ ← ステータスを DB に保存
 │     (定期送信)               │   Dashboard にリアルタイム反映
 │                              │
 │<─── ReceiveMessage ─────────│ ← サーバからのプッシュ通知
 │<─── ReceiveCommand ─────────│ ← リモートコマンド
 │──── CommandResult ──────────>│ ← コマンド実行結果
 │                              │
 │──── Disconnect ─────────────>│ ← 切断時 Status を Inactive に更新
```

### 9.2 接続グループ

- 各端末は `device_{DeviceId}` グループに参加
- グループ名がある端末は `group_{GroupName}` グループにも参加
- 全端末向けブロードキャストは `all_devices` グループ

---

## 10. セキュリティ考慮

| 項目 | 対策 |
|------|------|
| 端末認証 | DeviceId + API Key による認証 (将来的に証明書ベースも検討) |
| 通信暗号化 | HTTPS / WSS 必須 |
| パストラバーサル | ファイルストレージのパス検証（既存実装を踏襲） |
| 入力検証 | FluentValidation による全入力のバリデーション |
| レート制限 | API のレート制限ミドルウェア |

---

## 11. 非機能要件

| 項目 | 要件 |
|------|------|
| 対応端末数 | 〜1,000 台（SQLite のスケーラビリティ範囲） |
| ステータス更新間隔 | デフォルト 30 秒（コンフィグで変更可能） |
| データ保持期間 | ステータス履歴: 90 日（設定可能） |
| ログ保持期間 | 30 日ローテーション |
| 可用性 | 単一サーバ構成（SQLite 制約） |
| 監視 | Prometheus メトリクス + ヘルスチェック |

---

## 12. gRPC 通信 (デバイス ↔ サーバ)

### 12.1 概要

端末とサーバ間の通信は、SignalR に加えて gRPC もサポートする。
gRPC は Protocol Buffers によるスキーマ定義で型安全な通信を実現し、パフォーマンスに優れる。

SDK では `DeviceManagerClientOptions.UseGrpc = true` で gRPC モードに切り替え可能。

### 12.2 Proto 定義 (`DeviceManager.Shared/Protos/device_service.proto`)

```protobuf
service DeviceManagerService {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc ReportStatus(StatusReport) returns (StatusResponse);
  rpc SendMessage(DeviceMessage) returns (MessageResponse);
  rpc GetConfig(ConfigRequest) returns (ConfigResponse);
  rpc GetDataStoreValue(DataStoreRequest) returns (DataStoreResponse);
  rpc SetDataStoreValue(DataStoreSetRequest) returns (DataStoreResponse);
  rpc Subscribe(SubscribeRequest) returns (stream ServerEvent);
}
```

### 12.3 サーバストリーミング (Subscribe)

- 端末は `Subscribe` RPC でサーバイベントストリームに接続
- サーバは `GrpcEventDispatcher` を通じて各端末にイベントを配信
- イベント種別: `ReceiveMessage`, `ReceiveCommand`, `ConfigUpdated`
- 切断時は自動的に端末ステータスを Inactive に更新

### 12.4 gRPC ↔ SignalR 連携

gRPC 経由の端末操作は SignalR Hub を通じてダッシュボードにリアルタイム通知される。

```
gRPC端末 → DeviceGrpcService → Services → IHubContext<DeviceHub> → Blazor Dashboard
```

---

## 13. Swagger / OpenAPI

- Swagger UI: `/swagger`
- 全 REST API エンドポイントが自動ドキュメント化
- 開発環境・本番環境ともに利用可能
- 使用パッケージ: `Swashbuckle.AspNetCore`

---

## 14. シードデータ

初回起動時に以下のダミーデータが自動投入される（Device テーブルが空の場合のみ）：

| DeviceId | Name | Platform | Group | Status |
|----------|------|----------|-------|--------|
| device-001 | Device-001 | Android | Warehouse | Active |
| device-002 | Device-002 | Android | Warehouse | Active |
| tablet-a | Tablet-A | iOS | Sales | Active |
| tablet-b | Tablet-B | iOS | Sales | Warning |
| kiosk-tokyo-01 | Kiosk-Tokyo-01 | Windows | Retail-JP | Active |
| kiosk-osaka-01 | Kiosk-Osaka-01 | Windows | Retail-JP | Warning |
| sensor-hq-01 | Sensor-HQ-01 | Linux | HQ | Active |
| sensor-hq-02 | Sensor-HQ-02 | Linux | HQ | Error |
| rpi-lab-01 | RPi-Lab-01 | Linux | R&D | Inactive |
| field-unit-01 | Field-Unit-01 | Android | Field | Active (disabled) |

共通コンフィグ 7 件、共通データストア 4 件も同時投入。

---

## 15. WPF テストクライアント (DeviceManager.TestClient)

SDK の動作確認用 WPF アプリケーション。

### 15.1 機能

| タブ | 機能 |
|------|------|
| Status | ステータス手動送信、30秒間隔の自動レポート開始/停止 |
| Messages | メッセージ送受信、受信メッセージ一覧 |
| Config | サーバからコンフィグ取得・表示 |
| DataStore | KV の Get/Set/Delete/GetAll 操作 |
| Commands | サーバからのコマンド受信ログ |

### 15.2 接続設定

- Server URL / Device ID / Device Name を指定して Connect
- 接続状態インジケーター（Green/Orange/Yellow/Gray）
- ログ出力パネル（全操作のログ表示）

### 15.3 プロバイダー実装

| クラス | 実装内容 |
|--------|---------|
| `TestDeviceInfoProvider` | マシン名ベースの Device ID/Name、Platform="WPF-Windows" |
| `TestStatusProvider` | UI から Level/Progress/Battery を設定可能 |
| `TestCommandHandler` | 受信コマンドをイベントで通知、常に Success を返す |
