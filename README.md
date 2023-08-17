# Mr.Jack
>2인용 보드게임 Mr.Jack을 Unity 2D로 만들었습니다.  
>Photon을 이용해 2명이 멀티플레이가 가능합니다.

### 플레이 영상  
<p>  

  https://github.com/somi014/Mr.Jack_Portfolio/assets/96328947/066d50f4-b604-45bc-8abc-0f5e582aef77

</p>

***
### 1. 개요 
- 타이틀 : Mr.Jack </br>
- 엔진 : Unity</br>
- 플랫폼 : PC</br>
- 제작 기간 : 2023년 03월 20일 ~ 2023년 05월 10일</br>

### 2. 사용기술
- Photon
- ScriptableObject

### 3. 구현
<details>
  <summary>멀티플레이</summary>
  <!--summary 아래 빈칸 공백 두고 내용을 적는공간-->

  - 방 최대 인원(2명)이 되었는지 MasterClient에서 확인 후 대기 또는 게임을 시작합니다.</br>
  - CustomProperties로 데이터를 Hashtable에 저장해 두 플레이어가 같은 데이터 값을 가질 수 있도록 했습니다.</br>
</details>
<details>
  <summary>캐릭터 능력</summary>
  <!--summary 아래 빈칸 공백 두고 내용을 적는공간-->
  
  - 능력이 다른 총 8명의 캐릭터의 데이터는 ScriptableObject로 저장하고 있습니다.
</details>
