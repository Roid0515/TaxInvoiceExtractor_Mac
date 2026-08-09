# 전자세금계산서 변환기 — macOS Apple Silicon

전자세금계산서 PDF가 들어 있는 폴더를 선택해 데이터를 추출하고 표준 `.xlsx` 파일로 저장하는 macOS용 앱입니다. 생성된 파일은 Apple Numbers와 Mac용 Microsoft Excel에서 열 수 있습니다.

## 대상 환경

- Apple Silicon(M1/M2/M3/M4 계열)
- macOS 12 이상
- 앱 버전 2.0.0
- Developer ID 서명 및 Apple 공증 없음

## Mac에서 DMG 만들기

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)를 설치합니다.
2. 터미널에서 이 폴더로 이동합니다.
3. 다음 명령을 실행합니다.

```bash
bash build-macos.sh
```

완료된 파일은 다음 위치에 생성됩니다.

```text
artifacts/dist/TaxInvoiceExtractor_Mac_v2_AppleSilicon.dmg
```

DMG를 열고 `전자세금계산서 변환기.app`을 Applications 폴더로 드래그합니다. Apple Silicon의 실행 요건을 충족하기 위해 빌드 과정에서 인증서가 필요 없는 로컬 임시(ad-hoc) 서명만 적용하며, Developer ID 서명이나 Apple 공증은 수행하지 않습니다.

## 미서명 앱 최초 실행

앱이 Developer ID로 서명·공증되지 않았기 때문에 처음에는 Finder의 Applications 폴더에서 앱을 `우클릭 → 열기`로 실행해야 합니다. 그래도 차단되는 경우 다음 명령으로 이 앱의 격리 속성만 제거한 뒤 다시 실행합니다.

```bash
xattr -dr com.apple.quarantine "/Applications/전자세금계산서 변환기.app"
```

## 개발 및 테스트

```bash
dotnet restore TaxInvoiceExtractor.Mac.csproj --configfile NuGet.Config
dotnet test tests/TaxInvoiceExtractor.Mac.Tests.csproj
dotnet run --project TaxInvoiceExtractor.Mac.csproj
```

로그는 macOS의 다음 경로에 저장됩니다.

```text
~/Library/Application Support/TaxInvoiceExtractor/logs/
```

## GitHub에서 자동 DMG 생성

이 폴더를 독립 GitHub 저장소의 루트로 업로드하면 `.github/workflows/build-macos-dmg.yml`을 사용할 수 있습니다. Actions 화면에서 `Build Apple Silicon DMG` 워크플로를 수동 실행하거나 `v*` 태그를 푸시하면 macOS 빌드 머신이 테스트 후 DMG를 생성해 아티팩트로 제공합니다.

## Windows에서 가능한 검증 범위

Windows에서도 소스 빌드, 자동 테스트, `osx-arm64` 게시물 생성까지 가능합니다. 다만 `.app` 실행 권한 설정, `hdiutil` 기반 DMG 생성, Numbers/Excel 호환성의 실제 앱 실행 검증은 macOS에서 수행해야 합니다.
