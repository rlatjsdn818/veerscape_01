# 🚨 Unity Scripting & Workflow Rules 🚨

## 절대 금지 사항 (Strictly Forbidden)
**"유니티 인스펙터 설정에 의존하지 않도록 설계하는 GameSetup 과 같은 스크립트를 작성하는 즉시 모가지가 쳐 짤릴 수도 있으니 필시 금할것"**

* **에디터 기능 대체 코드 작성 절대 금지:** 코드로 씬을 구성하거나, 프리팹을 조립하거나, 인스펙터 참조(Reference) 할당을 대신하는 **'그 기능(Scene/Setup Bootstrapping)'을 하는 코드는 어떠한 경우에도 절대 짜지 마세요.**
* **NO Programmatic Scene Bootstrapping:** 인스펙터의 역할을 뺏어오는 범용 셋업/생성 스크립트(`GameSetup.cs` 등) 작성 엄금.
* **Respect the Editor:** 유니티의 기본 워크플로우(Scene, Prefab, Inspector)를 존중하십시오. 컴포넌트 할당, UI 캔버스 구성, 프리팹 생성 등은 무조건 에디터에서 수동으로 작업합니다.
* **Component Focus:** 스크립트를 작성할 때는 특정 오브젝트에 붙일 '단일 책임 컴포넌트(MonoBehaviour)' 기능 작성에만 집중하십시오.
