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
