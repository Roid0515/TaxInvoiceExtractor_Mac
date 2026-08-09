# Windows 교차 빌드 검증 기록

- 검증일: 2026-08-09
- 대상 RID: `osx-arm64`
- 게시 방식: self-contained, single-file
- 실행파일: `TaxInvoiceExtractorMac`
- 파일 크기: 99,110,490 bytes
- Mach-O 헤더: `CF FA ED FE 0C 00 00 01` (`ARM64`)
- SHA-256: `2975DC5C47194A77C798C62256A6007A30C62C2DBA266C545D9239DBB7D7AFD2`
- 자동 테스트: 15/15 통과
- Windows Avalonia UI 시작 테스트: 통과
- `build-macos.sh` Bash 문법 검사: 통과

실제 `.app` 실행, Finder 설치, Numbers 및 Mac용 Excel 열기, `hdiutil` DMG 생성은 Apple Silicon Mac에서 최종 확인해야 합니다.

## Apple Silicon Mac 최종 패키징 검증

- 검증일: 2026-08-10
- 환경: Apple Silicon (`arm64`), macOS 26.5.2
- SDK: .NET SDK 8.0.129, .NET Runtime 8.0.29
- 자동 테스트: 15/15 통과
- 앱 실행 및 시작 화면 UI: 통과
- 실행파일 형식: Mach-O 64-bit executable arm64
- 앱 서명: ad-hoc, `codesign --verify --deep --strict` 통과
- DMG 무결성: `hdiutil verify` 통과
- DMG 내부 구조: 앱 번들 및 Applications 링크 확인
- DMG 크기: 48,767,563 bytes
- DMG SHA-256: `6c7f4bb3d7dc03018196fa4261a959ed8759f267ae52e2ea963d22107cb89ed6`

Developer ID 서명과 Apple 공증은 적용하지 않았습니다. 실제 샘플 PDF 처리와 생성된 파일의 Numbers/Microsoft Excel 호환성은 샘플 파일을 사용해 별도로 확인해야 합니다.
