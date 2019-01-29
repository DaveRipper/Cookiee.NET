# Cookiee.NET
[쿠키](https://www.cookiee.net/) API의 C# 라이브러리입니다.

## 지원중인 기능
- 쿠키 스코어보드
- 일반 로그인 / 소셜 로그인
- 게임 데이터 관리
- 비동기 지원

## 설치
[쿠키 마켓](https://www.cookiee.net/cookie_market)에서 500P로 구매할 수 있습니다.

## 사용

설명에 앞서, 해당 글을 읽고 계신 분들은 [쿠키](https://www.cookiee.net/)에서 기본적인 강좌를 모두 한 번씩 읽고 오셨다고 가정하고 설명을 진행합니다.

### 스코어보드 API
먼저 [쿠키 스코어보드](https://www.cookiee.net/gmscore)에서 토큰과 시크릿을 발급받습니다.

발급받은 토큰과 시크릿으로 Scoreboard 객체를 생성합니다.
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

발급받은 토큰으로 Login 객체를 생성합니다.
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
```c#
var result3 = login.SocialLogin();
//비동기
var result4 = await login.SocialLoginAsync();
```

로그인을 성공했을 시 반환받는 유저 SRL을 통해 유저의 정보를 받아올 수 있습니다. 받아온 유저 정보에는 `유저 SRL, ID, 닉네임, 프로필 사진 경로`가 포함됩니다.
```c#
var user = login.GetUser(USERSRL);
//비동기
var user2 = await login.GetUserAsync(USERSRL);
```

### 데이터 API
먼저 [쿠키 게임 관리](https://www.cookiee.net/gmdata)에서 토큰과 시크릿을 발급받습니다.

발급받은 토큰과 시크릿으로 GameData 객체를 생성합니다.
```c#
var data = CookieeClient.CreateGameData(TOKEN, SECRET);
```
