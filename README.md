# DeviceManager

MAUI アプリケーション端末の管理サーバー。template-blazor-server をベースに構築。

- メトリクス表示(ダッシュボード)/ ログ / エラーレポート / ストレージ / メッセージ(プッシュ)/ Function(モック JSON)/ 設定管理
- 端末組み込み用のクライアントライブラリ(SDK)と WPF テストクライアント付き

詳細ドキュメント: [機能一覧](docs/features.md) / [画面一覧](docs/screens.md) / [DB 構造](docs/database.md) / [SDK 機能一覧](docs/sdk.md)

## 構成

| プロジェクト | 内容 |
|---|---|
| DeviceManager.AppHost | .NET Aspire ホスト |
| DeviceManager.Contracts | 端末⇔サーバー共有のモデル・定数 |
| DeviceManager.Core | サービス / データアクセス(Smart.Data.Accessor + SQLite)/ ストレージ |
| DeviceManager.Server | Blazor Server 管理画面 + Minimal API + SignalR ハブ |
| DeviceManager.Client | 端末向け SDK(SignalR + REST) |
| DeviceManager.TestClient | WPF テストクライアント |

`__Old` / `__Sandbox` は旧実装・プロトタイプの残骸(参照用)。

## 起動

```
dotnet run --project DeviceManager.Server
```

- 管理画面 / REST / SignalR: `http://localhost:8082/`(HTTP/1.1。初期アカウント: admin / admin — **`appsettings.json` の `Auth:InitialPassword` は必ず変更すること**)
- テレメトリ gRPC: `http://localhost:8083/`(HTTP/2 専用ポート。平文 h2c のため別ポート)
- 端末 API / ハブ / gRPC: `X-Api-Key` ヘッダによる共有キー認証(`Device:ApiKey` — **こちらも必ず変更すること**)
- Aspire: `dotnet run --project DeviceManager.AppHost`
- 動作確認: `dotnet run --project DeviceManager.TestClient`(WPF。接続先・gRPC URL・キー・タンキング方式を選んで接続)

## 通信方式

| 経路 | 方式 |
|---|---|
| **テレメトリ(メトリクス / ログ / クラッシュレポート)** | **gRPC**(`TelemetryService`、8083)。SDK 側は非同期チャネル + タンキング + 自動再送 |
| 端末 → サーバー(登録 / メッセージ) | SignalR `/hubs/device` |
| サーバー → 端末(メッセージ / コンフィグ再読込) | SignalR コールバック(ReceiveMessage / ConfigReload) |
| ストレージ(ファイル送受信)/ コンフィグ取得 | HTTP(REST、`X-Api-Key`) |
| 管理画面のリアルタイム更新 | プロセス内イベントバス(DeviceEventBus) |

テレメトリは送信エラー時にタンキングプロバイダー(オンメモリ / SQLite。`ITelemetryStore` で拡張可)へ退避し、バックオフ付きで自動再送する(persist-then-send / at-least-once)。詳細は [SDK 機能一覧](docs/sdk.md)。

## API 概要

| メソッド / パス | 認証 | 内容 |
|---|---|---|
| gRPC `TelemetryService`(8083) | ApiKey | ReportStatus / SendLogs / SendCrashReport(SDK の標準経路。未登録端末は自動登録) |
| Hub `/hubs/device` | ApiKey | Register / SendMessage(下り: ReceiveMessage / ConfigReload) |
| GET `/api/devices/` `/api/devices/summary` | Cookie | 端末一覧 / サマリー |
| POST `/api/devices/{id}/status` | ApiKey | REST でのステータス報告(外部連携用) |
| POST `/api/logs/batch` / GET `/api/logs/` | ApiKey / Cookie | ログ一括受信(外部連携用)/ 照会 |
| POST・GET・DELETE `/api/reports/...` | ApiKey / Cookie | エラーレポート(POST は外部連携用) |
| GET・POST・DELETE `/api/storage/{**path}`、PUT `/api/storage/mkdir/{**path}` | ApiKey または Cookie | 一覧(末尾 `/`)/ DL / UP / 削除 / フォルダ作成 |
| GET `/api/messages/` / POST `/api/messages/send` | Cookie | メッセージ履歴 / 送信 |
| GET `/api/config/devices/{id}/resolved` | ApiKey または Cookie | 解決済みコンフィグ |
| GET・POST `/api/function/{name}` | ApiKey または Cookie | モック JSON 返却 / エコー |
| `/auth/login` `/auth/logout`、`/health` `/alive` | - | 管理ログイン / ヘルスチェック |

開発時のみ `/openapi/v1.json` / `/swagger` / `/redoc` を公開。
