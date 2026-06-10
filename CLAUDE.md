# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 概要

このリポジトリは「かざぐるマウス」（KazaguruMouse64）のバイナリ配布物です。ソースコードは含まれていません。

- **用途**: Windows 用マウス拡張ユーティリティ（マウスジェスチャー、スクロール強化など）
- **作者**: Mitsuhal (Static Flower)
- **ライセンス**: フリーウェア

## ファイル構成

| ファイル | 説明 |
|---|---|
| `Kazaguru.exe` | メイン実行ファイル |
| `Kazasub.dll` | サブ DLL |
| `Kazawow64.exe` | WOW64（32/64ビット橋渡し）ヘルパー |
| `hook\1.6.7.762\Kazahook.dll` | 64ビット用フック DLL |
| `hook\1.6.7.762\Kazahook32.dll` | 32ビット用フック DLL |
| `Kazaguru.chm` | ヘルプファイル |

## 注意事項

- このリポジトリにはビルドシステム・ソースコードが存在しないため、コンパイルや修正は行えません。
- 使用方法は `Kazaguru.chm` を参照してください。
- 再配布時は `ReadMe.txt` 記載の条件を確認してください。
