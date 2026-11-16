# Song Unity Popup

CrawlStars의 Popup 시스템을 분리한 로컬 UPM 패키지다. 현재 프로젝트에는 적용하지 않았다.

## 포함 파일

| 경로 | 내용 |
|---|---|
| `Runtime/` | `PopupManager`, `PopupHandler`, `CommonPopup`, `OneButtonPopup`, `TwoButtonPopup` |
| `Runtime/Resources/` | 기존 GUID를 유지한 BasePopup, OneButtonPopup, TwoButtonPopup 프리팹 |
| `Runtime/Defaults/` | Unity TMP 기본 Liberation Sans와 기본 렌더링에 필요한 파일 |
| `Tests/` | EditMode 프리팹 검사와 PlayMode 수명주기 테스트 |
| `Documentation`으로 시작하는 폴더 | 이전 절차, 의존성 분석, 검증 결과 |
| `Tools`로 시작하는 폴더 | 원본을 수정하지 않는 추출·검증 보조 도구 |

문서·도구 폴더 이름의 마지막 문자 U+007E는 Unity가 폴더를 임포트하지 않도록 하는 규칙이다. 게임 전용 팝업은 패키지에 포함하지 않고 원래 프로젝트에 남긴다.

기본 UI는 단색 배경·버튼과 Unity TMP 기본 폰트인 Liberation Sans를 사용한다. 프로젝트 전용 폰트, 이미지, outline material은 포함하지 않는다.

## 다른 프로젝트에서 사용

1. `com.song.unity-popup` 폴더 전체를 원하는 위치로 복사한다. `Validation`으로 시작하는 임시 검증 폴더가 있으면 제외해도 된다.
2. Unity 6000.3 이상과 uGUI 2.0.0을 사용한다. 검증 기준은 CrawlStars의 **6000.3.15f1**이다.
3. 먼저 Package Manager의 **Install package from git URL**에서 UniTask를 설치한다.

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10
```

4. **Install package from disk**에서 이 폴더의 `package.json`을 선택한다. 또는 대상 프로젝트의 `Packages/com.song.unity-popup`에 폴더를 넣어 embedded package로 사용한다.
5. TMP를 처음 사용하는 프로젝트는 **Window > Text Mesh Pro > Import TMP Essential Resources**에서 Unity 기본 설정을 준비한다. CrawlStars에는 이미 준비되어 있다.
6. 씬에 EventSystem과 현재 프로젝트 입력 방식에 맞는 UI Input Module을 둔다. ESC 입력 처리는 호출 측 책임이다.

UniTask는 기본 Unity Registry에 없으므로 패키지의 버전 의존성만으로 Git 저장소가 자동 설치되지 않는다. CrawlStars에는 이미 같은 버전이 설치되어 있다. TMP 코드는 Unity 6의 `com.unity.ugui`에 포함되어 별도 `com.unity.textmeshpro` 패키지가 필요하지 않다. 기본 팝업 폰트와 셰이더는 패키지에 포함하며, 프로젝트 전역 TMP 설정은 호스트의 TMP Essential Resources를 사용한다.

```csharp
using Cysharp.Threading.Tasks;
using Popup;

var result = await PopupManager.Instance.ShowAsync(
    nameof(TwoButtonPopup),
    new TwoButtonPopup.Param("Exit", "Are you sure you want to exit?"));

if (result is TwoButtonPopup.Result { isClickedOk: true }) {
    // Confirmed.
}
```

## CrawlStars에서 나중에 교체

**원본과 패키지를 그대로 동시에 설치하면 클래스·GUID·Resources 이름이 중복된다.** 원본을 유지한 지금 상태에서는 이 폴더를 설치하지 않는다. 실제 교체 시에는 `Documentation` 폴더의 `MIGRATION.md`에 지정한 **공용 스크립트 5개와 프리팹 3개만** 메타 파일까지 함께 교체한다. 게임 전용 팝업과 공용 이미지·폰트 원본은 남긴다.

기존 네임스페이스, 공개 메서드, 중첩 Param/Result, 직렬화 필드명과 스크립트 GUID를 유지한다. 따라서 게임 전용 팝업은 패키지의 `PopupHandler`를 그대로 상속한다. DOTween, 네트워크, 게임 매니저는 공용 패키지 의존성이 아니다.

## 테스트

테스트를 실행할 프로젝트에는 Unity Test Framework가 필요하다. 로컬 UPM 패키지 테스트를 활성화할 때 호스트 `Packages/manifest.json`의 최상위 `testables` 배열에 `com.song.unity-popup`을 추가하고 Test Runner에서 `Song.UnityPopup` 테스트를 실행한다. 현재 CrawlStars 원본의 manifest는 변경하지 않았다.

```powershell
unity test <검증프로젝트경로> --mode EditMode --filter Song.UnityPopup --output editmode.xml
unity test <검증프로젝트경로> --mode PlayMode --filter Song.UnityPopup --output playmode.xml
```

구체적인 실행 결과와 제한은 `Documentation` 폴더의 `VALIDATION.md`를 확인한다.
