# GOS (Game Object Scripting) 프레임워크 분석 보고서

GOS(Game Object Scripting)는 유니티(Unity)의 게임 오브젝트와 컴포넌트 구조를 기반으로, 복잡한 코드 작성 없이 인스펙터 상에서의 설정과 오브젝트 배치만으로 다양한 기믹 및 레벨 디자인 요소를 유연하게 구현할 수 있도록 설계된 플러그인 형태의 비주얼 스크립팅 프레임워크입니다.

---

## 1. 프레임워크 핵심 아키텍처 (Core Architecture)

GOS의 아키텍처는 매우 단순하면서도 확장성이 높은 **"상태 기반 이벤트 전파 모델(State-based Event Propagation Model)"**을 채택하고 있습니다. 모든 노드는 유니티의 `MonoBehaviour`를 상속받아 씬 내의 게임 오브젝트에 부착될 수 있으며, 상호 참조를 통해 흐름을 형성합니다.

### 1.1 `ProcessBase` (기반 클래스)
모든 GOS 컴포넌트의 최상위 추상 클래스로, 상태 관리와 실행 표준을 정의합니다.
* **`IsOn` (프로퍼티):** 현재 해당 프로세스의 상태가 활성화(`true`)되었는지 비활성화(`false`)되었는지를 나타내는 핵심 상태 변수입니다. 다른 노드들이 이 값을 참조하여 실행 여부를 결정합니다.
* **`Execute()` (추상 메서드):** 자식 클래스에서 구현하는 핵심 메서드로, 노드 고유의 액션이나 조건 평가 로직이 포함됩니다.
* **`Reset()`:** 상태를 비활성화(`IsOn = false`)로 초기화합니다.
* **`CheckInputProcessStatus(ProcessData)`:** 특정 입력 프로세스의 `IsOn` 상태를 읽어오며, 직렬화된 `isNot` 값에 따라 상태를 논리 반전(NOT 연산)하여 반환할 수 있도록 유틸리티 기능을 제공합니다.
* **`ForceExecute()` (에디터 전용):** 컨텍스트 메뉴(`강제 실행 (테스트)`)를 지원하여, 게임 플레이 중에 개발자가 인스펙터 우클릭을 통해 개별 노드의 기능을 수동으로 강제 트리거하고 테스트할 수 있도록 지원합니다.

### 1.2 `ProcessData` (연결 구조체)
노드와 노드 사이의 연결 정보(에지, Edge)를 직렬화하기 위한 데이터 구조체입니다.
* **`process` (`ProcessBase`):** 가리키고자 하는 대상 GOS 노드 컴포넌트.
* **`isNot` (`bool`):** 연결된 대상 상태의 반전 여부 (예: 대상이 꺼져 있을 때 작동하게 하려면 `true`로 설정).

---

## 2. 노드 컴포넌트 분류 및 기능 (Node Components)

GOS 프레임워크는 기능적 역할에 따라 크게 **입력(Inputs), 처리/논리(Logics), 출력(Outputs)**으로 나뉩니다.

### 2.1 입력 노드 (Inputs) - 흐름의 시작점
이벤트의 발생 원인을 감지하고 이벤트를 시작하거나 상태를 변화시키는 노드들입니다.

| 컴포넌트명 | 설명 | 주요 변수 및 동작 |
| :--- | :--- | :--- |
| **`InputEvent`** | 유저의 키 입력을 감지해 이벤트를 발생시킵니다. | `triggerKey` (기본 마우스 좌클릭), `outputs` (수행할 프로세스 목록). 키 다운 시 `Execute()`를 통해 출력 노드들을 실행하고 `IsOn = true`로 설정합니다. |
| **`Trigger`** | 유니티 `Collider`의 Trigger 충돌을 기반으로 특정 대상의 진입/이탈을 감지합니다. | `selectedTag` (감지할 태그), `objTarget` (감지된 임시 오브젝트 캐시). 진입 시 `IsOn = true` 및 타겟 저장, 이탈 시 타겟 제거. |
| **`TriggerEnter`** | 콜라이더 영역 진입 순간만 감지합니다. | `selectedTag`, `outputs`. 감지 시 `IsOn = true` 및 출력 실행. |
| **`TriggerExit`** | 콜라이더 영역 탈출 순간만 감지합니다. | `selectedTag`, `outputs`. 감지 시 `IsOn = true` 및 출력 실행. |
| **`TriggerTimmer`** | 트리거 내 잔류 시간에 따른 타이머 감지기입니다. | 진입 후 설정 시간 동안 잔류하면 작동합니다. |
| **`Timer`** | 지연 실행이나 일정 시간 대기 제어를 지원합니다. | `seconds` (대기 시간). `Execute()` 시 코루틴을 통해 지정 시간 대기 후 `IsOn = true`로 변경하여 이벤트를 지연 전파합니다. |
| **`IsDestroy`** | 지정한 오브젝트들이 완전히 파괴되었는지를 체크합니다. | `cach` (참조할 `GlobalGameObject` 리스트). 등록된 대상들의 `OnObjectDestroyed` 이벤트를 모니터링하여, 모두 파괴(`count <= 0`)되면 `IsOn = true`로 전환됩니다. |
| **`OnEvent`** | 특정 커스텀 이벤트가 호출되는 시점을 감지하는 노드입니다. | 외부 액션이나 시스템 신호로부터 진입합니다. |

### 2.2 처리/논리 노드 (Logics) - 흐름의 제어 및 조건 검사
입력 상태를 조합하거나 흐름 분기, 반복 처리를 수행합니다.

* **`Conditional` (조건분기):**
  * `inputData` (`ProcessData`) 상태가 `true`가 되는 순간 한 번만 `outputData` 리스트에 정의된 하위 프로세스들을 순차 실행합니다.
  * `isLoop`를 체크할 경우 실행 후 즉시 입력 노드와 하위 출력 노드들의 상태를 `Reset()`시켜 다음 트리거가 가능하도록 순환 구조를 만들어줍니다.
  * **안전장치:** 무한 루프(`StackOverflow`) 유발을 방지하기 위해, 에디터 OnValidate 단계에서 자가 참조(Self-Reference)를 감지하고 경고창을 띄우며 강제로 연결을 차단하는 코드가 기본 내장되어 있습니다.
* **`Logic` (논리 게이트):**
  * 두 개의 입력 프로세스(`processes1`, `processes2`)의 상태 값을 논리 연산자로 연산하여 `IsOn`에 기록합니다.
  * **`LogicType.And`:** 두 입력 노드가 모두 `IsOn == true` 일 때만 결과가 `true`가 됩니다.
  * **`LogicType.Or`:** 두 입력 노드 중 하나만 `true`여도 결과가 `true`가 됩니다.
* **반복문 (`ForLoop`, `WhileLoop`):**
  * 특정 조건 동안이나 지정된 횟수만큼 자식 프로세스들을 반복 실행하도록 하는 제어 흐름 노드들입니다.

### 2.3 출력 노드 (Outputs) - 액션 수행
최종적으로 씬의 상태를 물리적/논리적으로 바꾸는 핵심 액터들입니다.

* **오브젝트 활성 상태 변경:**
  * `Activate`: 대상 GameObject를 `SetActive(true)`로 활성화합니다.
  * `Deactivate`: 대상 GameObject를 `SetActive(false)`로 비활성화합니다. (자기 자신을 비활성화하는 경우 안전을 위해 `OnDisable` 라이프사이클에서도 자체 강제 실행 로직을 가집니다.)
  * `ToggleActivate`: 활성화 상태를 온오프 토글합니다.
* **물리적 연출 및 연동:**
  * `MoveObject` / `MoveTo` / `Teleport`: 지정한 Transform이나 좌표로 오브젝트를 선형 보간 이동 또는 즉시 순간이동시킵니다.
  * `RotateObject` / `RotateDoor` / `SlidingDoor`: 게임 기믹의 핵심이 되는 문 열림(회전/슬라이딩) 연출 및 오브젝트 회전을 지원합니다.
  * `PlayAnimation`: 대상 `Animator`를 제어해 지정 트랜지션 및 스테이트 재생을 트리거합니다.
  * `CameraControl`: 시네머신(Cinemachine) 카메라 전환을 매핑하여 특정 씬 연출 및 컷씬을 생성할 수 있습니다.
  * `LightingControl`: 씬 라이트의 밝기나 색상 등을 동적으로 조절합니다.
* **오브젝트 생명주기 제어:**
  * `Instance`: 프리팹을 동적으로 생성하고, 생성된 인스턴스를 `IsDestroy`와 같은 다른 감지 노드에 실시간 바인딩해 파괴 여부를 연속적으로 모니터링할 수 있도록 설계되었습니다.
  * `Destroy`: 실행 시 특정 GameObject를 완전히 씬에서 삭제(`Destroy`)시킵니다.
* **기타 유틸리티:**
  * `EntryModifierEvent`: 다른 노드의 상태나 이벤트를 변경 및 튜닝합니다.
  * `Register` / `Unregister`: 상호작용 관련 이벤트 등록/해제를 처리합니다.
  * `GameTimeAdjust`: 타임스케일(배속) 제어나 타임 아웃 등을 조정합니다.

---

## 3. 에디터 편의성 및 생산성 확장 (Editor Tooling)

GOS의 강력한 장점 중 하나는 유니티 에디터와의 유기적인 통합입니다.

### 3.1 `VisualScriptingEditor`
* 유니티의 Hierarchy 뷰 빈 공간 우클릭 메뉴 또는 상단 `GameObject/VisualScripting/` 메뉴를 확장하여, 복잡한 컴포넌트 부착 과정 없이 한 번의 클릭만으로 프리셋 게임 오브젝트(예: 콜라이더가 이미 충돌체/트리거 처리된 Trigger 오브젝트 등)를 생성할 수 있도록 지원합니다.
* 유니티 에디터의 Undo 시스템에 생성 작업을 등록하여, 실수 시 되돌리기(`Ctrl + Z`)가 완벽하게 작동합니다.

### 3.2 `VisualScriptingPalette` (`EditorWindow`)
* 개발자가 상단 메뉴 `Visual Scripting/GameObject/Pallete Window`를 통해 열 수 있는 전용 그래픽 도구 팔레트 창입니다.
* **자동 노드 수집:** 리플렉션 기술(`TypeCache.GetTypesDerivedFrom<ProcessBase>()`)을 사용하여, 프로젝트 내에 새롭게 코딩하여 추가된 모든 `ProcessBase` 파생 클래스를 자동으로 실시간 탐색합니다.
* **폴더 기반 자동 카테고리 분류:** 스크립트 파일이 저장된 실제 디렉토리 경로 구조(예: `Input`, `Logic`, `Output`)를 파싱하여 팔레트 창 내부에서 분류(📂 카테고리) 및 순서대로 보기 좋게 정렬합니다.
* **검색 창 지원 (`_searchQuery`):** 많은 수의 노드 중 원하는 노드를 이름으로 빠르게 타이핑 필터링할 수 있으며, `X` 버튼으로 입력을 쉽게 지우는 편의 기능도 가지고 있습니다.
* **원클릭 생성:** `➕ [노드명]` 버튼을 클릭하면 활성화된 게임오브젝트 아래에 자식으로 노드가 부착되며 자동 컴포넌트 할당(Collider 트리거 설정 등 포함) 및 포커싱이 수행됩니다.

### 3.3 `ProcessBaseEditor` (`CustomEditor`)
* 모든 GOS 컴포넌트(`ProcessBase`를 상속한 클래스들)의 인스펙터 창 하단에 공통적으로 **"실행 (Execute)" 커스텀 버튼**을 렌더링합니다.
* 이를 통해 개발자가 씬 플레이 도중 이벤트 동작 과정을 보기 위해 매번 트리거 조건을 수동으로 맞출 필요 없이, 원하는 시점에 개별 노드의 행동(문 열기, 카메라 전환 등)을 인스펙터에서 즉석으로 트리거해볼 수 있습니다.

---

## 4. 데이터베이스 및 부가 시스템 (Supporting Systems)

VisualScripting 시스템 하부에는 게임의 기초 스탯과 로직을 지원하는 별도의 컴포넌트 그룹이 존재합니다.

### 4.1 데이터베이스 & CSV 파싱 (`CustomData`)
* **`CSVParser` & `RowEntity`:** 지정한 CSV 파일을 파싱해 데이터를 행/열 단위 엔티티 구조체로 구성합니다.
* **`TableData`:** 개별 데이터 테이블의 구조와 메타데이터를 유지합니다.
* **`DatabaseManager`:** 게임 데이터베이스를 싱글톤 구조로 로드하고 스탯 조회 인터페이스를 구현합니다.
* **`EntryCloner` & `EntryClonerEditor`:** 데이터베이스의 테이블 정보(몬스터/플레이어 기초 스탯 정보 등)를 가져와 실제 씬 오브젝트에 클로닝하거나 바인딩하는 유틸리티입니다.

### 4.2 인게임 디버깅 (`Debug`)
* **`VisualLogger` & `DebuggerMessage`:** 화면 상에 GOS 실행 상황을 로그 오버레이로 즉각 뿌려주거나 추적하는 도구입니다.
* **`RuntimeConsole`:** 런타임 환경에서 게임 테스트 중 콘솔 로그를 직접 모니터링할 수 있는 UI 캔버스 형태의 경량 콘솔입니다.
* **`BoxColliderVisualizer`:** 씬 뷰나 게임 뷰에서 눈에 보이지 않는 보이지 않는 트리거용 Box Collider 영역을 임의의 기즈모선 및 반투명 큐브로 그려주어 배치를 편리하게 돕습니다.

### 4.3 FSM 및 캐릭터 제어 (`Etc`)
* **`BaseFSM`:** 상태 머신(Finite State Machine)의 기반으로, 캐릭터 행동 양식을 유한 상태들로 제어하는 기본 클래스입니다.
* **`AIController` & `WaypointPath`:** 웨이포인트를 순회하거나 추적 상태를 정의한 기본형 AI 몬스터 컨트롤러입니다.
* **`PlayerController`:** 캐릭터의 8방향 이동 및 카메라 방향 정렬, 물리 입력 연동을 담당하는 스크립트입니다.
* **`ManagerBase` & `GameManager` / `ObjectPoolManager`:** 씬 전체 생명주기 관리 및 오브젝트 풀링 등을 지원합니다.

---

## 5. 종합 평가 및 강점 요약

1. **높은 컴포넌트 지향성 (Component-Oriented):** 노드 하나하나가 컴포넌트로 분리되어 있어 유니티 본연의 Hierarchy 관리 방식을 그대로 활용할 수 있어 디자이너 직관성이 높습니다.
2. **풍부한 에디터 자동화:** 리플렉션을 응용한 `VisualScriptingPalette`와 커스텀 인스펙터가 개발 파이프라인의 편의성을 대폭 보강하고 있습니다.
3. **확장 편의성:** 개발자가 C# 코드로 `ProcessBase`를 상속한 새로운 동작 클래스를 작성하기만 하면, 별도의 설정 없이 팔레트 창에 즉시 검색되어 노드로 바로 사용 가능한 플러그앤플레이(Plug-and-Play) 확장성을 보장합니다.
