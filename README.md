# Cookiee.NET
[쿠키](https://www.cookiee.net/) API의 C# 라이브러리입니다.

## 사용 조건
- .NET 4.5 / 4.6, .NET Standard 2.0
- 유니티의 경우 2018.1 버전 이상

## 지원중인 기능
- 쿠키 스코어보드
- 일반 로그인 / 소셜 로그인
- 게임 데이터 관리
- 비동기 지원

## 설치
[NuGet](https://www.nuget.org/packages/Cookiee.NET/)으로 설치할 수 있습니다. Cookiee.NET은 반드시 Newtonsoft.Json이 설치되어 있어야 작동합니다. Cookiee.NET을 설치하면 자동으로 Newtonsoft.Json도 설치됩니다.
```
PM> Install-Package Cookiee.NET
```

유니티의 경우 `Cookiee.NET.dll, Newtonsoft.Json.dll` 두 파일을 `Assets/Plugins` 폴더 안에 넣으면 됩니다.

## 사용
설명에 앞서, 해당 글을 읽고 계신 분들은 [쿠키](https://www.cookiee.net/)에서 기본적인 강좌를 모두 한 번씩 읽고 오셨다고 가정하고 설명을 진행합니다.

코드 상단에 다음 코드를 추가합니다.
```c#
using Cookiee;
```

### 스코어보드 API
먼저 [쿠키 스코어보드](https://www.cookiee.net/gmscore)에서 토큰과 시크릿을 발급받습니다.

발급받은 토큰과 시크릿으로 `Scoreboard` 객체를 생성합니다.
```c#
var board = CookieeClient.CreateScoreboard(TOKEN, SECRET);
```

스코어보드를 저장하는 방법은 다음과 같습니다.
```c#
var result = board.Add("NAME", SCORE, USERSRL, ISUPDATE);
//비동기
var result2 = await board.AddAsync("NAME", SCORE, USERSRL, ISUPDATE);
```

스코어보드를 불러오는 방법은 다음과 같습니다.
```c#
//최대 50개 불러오기
var result3 = board.Get();
//개수 지정해서 불러오기
var result4 = board.Get(10);
//비동기
var result5 = await board.GetAsync(5);
```

### 로그인 API
먼저 [쿠키 게임 관리](https://www.cookiee.net/gmdata)에서 토큰을 발급받습니다.

발급받은 토큰으로 `Login` 객체를 생성합니다.
```c#
var login = CookieeClient.CreateLogin(TOKEN);
```

일반 로그인을 하는 방법은 다음과 같습니다.
```c#
var result = login.NormalLogin("ID", "PW");
//비동기
var result2 = await login.NormalLoginAsync("ID", "PW");
```

소셜 로그인을 하는 방법은 다음과 같습니다. 비동기던 아니던, 꼭 브라우저 창을 열어야 합니다.

`GetSocialLoginUrl` 함수를 호출하면 소셜 로그인을 진행할 수 있는 주소가 반환됩니다. 플랫폼별 방식에 맞게 브라우저를 연 다음 `SocialLogin` 함수를 호출하면 됩니다.
```c#
//PC에서 브라우저 열기
System.Diagnostics.Process.Start(login.GetSocialLoginUrl());

var result3 = login.SocialLogin();
//비동기
var result4 = await login.SocialLoginAsync();
```

로그인을 성공했을 시 반환받는 `유저 SRL`을 통해 유저의 정보를 받아올 수 있습니다. 받아온 유저 정보에는 `유저 SRL, ID, 닉네임, 프로필 사진 경로`가 포함됩니다.
```c#
var user = login.GetUser(USERSRL);
//비동기
var user2 = await login.GetUserAsync(USERSRL);
```

### 데이터 API
먼저 [쿠키 게임 관리](https://www.cookiee.net/gmdata)에서 토큰과 시크릿을 발급받습니다. 로그인이 필요합니다.

발급받은 토큰과 시크릿으로 `GameData` 객체를 생성합니다.
```c#
var data = CookieeClient.CreateGameData(TOKEN, SECRET);
```

데이터 API를 사용하는 데에는 유저 SRL이 필요합니다. 데이터를 불러오는 방법은 다음과 같습니다.
```c#
var userData = data.Load(USERSRL);
//비동기
var userData2 = await data.LoadAsync(USERSRL);
```

불러온 데이터는 `Dictionary` 형태로 반환됩니다. 이 데이터를 원하는 대로 작업한 뒤에 저장해주어야 합니다.
```c#
var result = data.Save(USERSRL, userData);
//비동기
var result2 = await data.SaveAsync(USERSRL, userData2);
```

`Save`/`SaveAsync` 메서드는 데이터를 추가하는 용도가 아닙니다. 말 그대로 데이터를 저장하는 용도입니다. 만약 A라는 유저의 데이터가 `B, C`가 있었는데 `Save` 메서드로 `D, E`를 저장한다면 A의 데이터는 `B, C, D, E`가 아닌 `D, E`가 됩니다.

하나의 데이터를 추가하고 싶을 때는 `Add` 메서드를 사용하면 됩니다. 이 메서드는 내부에서 `Save`와 `Load` 메서드를 호출합니다. 따라 성능 저하가 발생할 수 있을 수도 있습니다. (아직 확인 안 해봄)
```c#
var result3 = data.Add(USERSRL, KEY, VALUE);
//비동기
var result4 = await data.AddAsync(USERSRL, KEY, VALUE);
```

`Add` 메서드와 똑같은 구조지만 하나의 데이터를 삭제하는 용도인 `Delete` 메서드 또한 존재합니다.
```c#
var result5 = data.Delete(USERSRL, KEY);
//비동기
var result6 = await data.DeleteAsync(USERSRL, KEY);
```

`Add`, `Delete` 메서드는 다음 코드와 같은 기능을 합니다.
```c#
var userData = await data.LoadAsync(USERSRL);
userData.Add(KEY, VALUE);
var result = await data.SaveAsync(USERSRL, userData);
```

## 추후 지원 기능
- 자동 업데이터
