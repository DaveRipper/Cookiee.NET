using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Cookiee
{
    /// <summary>
    /// Cookiee의 예외 클래스입니다.
    /// </summary>
    [Serializable()]
    public class CookieeException : System.Exception
    {
        public CookieeException() : base() { }
        public CookieeException(string message) : base(message) { }
        public CookieeException(string message, System.Exception inner) : base(message, inner) { }

        protected CookieeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }

    /// <summary>
    /// Cookiee API를 사용하기 위한 가장 기본적인 클래스입니다.
    /// </summary>
    public class CookieeClient
    {
        /* 설계
         * 스코어보드같은 경우 Scoreboard 클래스가 있고 안에 메서드가 있으며, CookieeClient가 팩토리 메서드를 가져서 잘 호출하는 그런 방식인데
         * 로그인은 CookieeClient 자체에서 정적으로 처리해도 괜찮을..듯 싶었는데 Cookiee.NET은 모든 쿠키 기능을 구현하는 걸 목표로 한다
         * 겜스 소스 보니까 기능이 좀 더 있는데 다 구현할라면 로그인도 클래스로 빼서 팩토리 메서드 두는 게 나을 듯 싶다
         * 결론: 모든 기능은 별개 클래스로 두고 CookieeClient에서 팩토리 메서드로 반환하는 방식을 따르자
         * 리턴값은 익셉션으로 던지는 게 좋을 듯
        */

        /// <summary>
        /// 주어진 토큰과 시크릿의 스코어보드 객체를 생성합니다.
        /// </summary>
        /// <param name="token">사용하려는 대상 스코어보드의 토큰 값입니다.</param>
        /// <param name="secret">사용하려는 대상 스코어보드의 시크릿 값입니다.</param>
        /// <returns></returns>
        public static Scoreboard CreateScoreboard(string token, string secret) => new Scoreboard
        {
            Token = token,
            Secret = secret
        };
        /// <summary>
        /// 주어진 토큰의 로그인 객체를 생성합니다.
        /// </summary>
        /// <param name="token">사용하려는 대상 쿠키 게임 관리의 토큰 값입니다.</param>
        /// <returns></returns>
        public static Login CreateLogin(string token) => new Login
        {
            Token = token
        };
        /// <summary>
        /// 주어진 토큰과 시크릿의 게임 관리 객체를 생성합니다.
        /// </summary>
        /// <param name="token">사용하려는 대상 게임 관리의 토큰 값입니다.</param>
        /// <param name="secret">사용하려는 대상 게임 관리의 시크릿 값입니다.</param>
        /// <returns></returns>
        public static GameData CreateGameData(string token, string secret) => new GameData
        {
            Token = token,
            Secret = secret
        };
    }

    /// <summary>
    /// Cookiee 유저 클래스입니다.
    /// </summary>
    public class CookieeUser
    {
        /// <summary>
        /// 유저 SRL 값입니다.
        /// </summary>
        public int Serial { get; set; }
        /// <summary>
        /// 유저 ID입니다.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 유저 닉네임입니다.
        /// </summary>
        public string NickName { get; set; }
        /// <summary>
        /// 유저의 프로필 사진 경로입니다.
        /// </summary>
        public string ProfileImage { get; set; }

        public CookieeUser(int serial, string id, string nickName, string profileImage)
        {
            this.Serial = serial;
            this.ID = id;
            this.NickName = nickName;
            this.ProfileImage = profileImage;
        }
    }

    /// <summary>
    /// Cookiee 스코어보드의 점수 클래스입니다.
    /// </summary>
    public class Score
    {
        /// <summary>
        /// 순위 값입니다.
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// 해당 유저의 SRL 값입니다.
        /// </summary>
        public int UserSerial { get; set; }
        /// <summary>
        /// 해당 유저의 이름입니다.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 해당 유저의 점수입니다.
        /// </summary>
        public long UserScore { get; set; }
        /// <summary>
        /// 점수가 기록된 시간입니다.
        /// </summary>
        public DateTime Time { get; set; }

        public Score(int rank, int userSerial, string name, long userScore, DateTime time)
        {
            this.Rank = rank;
            this.UserSerial = userSerial;
            this.Name = name;
            this.UserScore = userScore;
            this.Time = time;
        }
    }

    /// <summary>
    /// Cookiee 스코어보드 클래스입니다.
    /// </summary>
    public class Scoreboard
    {
        /// <summary>
        /// 해당 스코어보드의 토큰입니다.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 해당 스코어보드의 시크릿입니다.
        /// </summary>
        public string Secret { get; set; }
        private readonly string scoreboardPostUrl = "https://www.cookiee.net/score/scoreHandlerV2.php";
        private readonly string scoreboardGetUrl = "https://www.cookiee.net/score/view_json";
        private readonly HttpClient client = new HttpClient();

        /// <summary>
        /// 점수를 비동기로 추가합니다.
        /// </summary>
        /// <param name="userName">추가할 이름입니다.</param>
        /// <param name="score">추가할 점수입니다.</param>
        /// <param name="memberSerial"></param>
        /// <param name="update"></param>
        /// <returns>추가한 점수의 순위를 반환합니다.</returns>
        public async Task<int> AddAsync(string userName, long score, int memberSerial, bool update)
        {
            //인수가 잘못됐는지 체크해줬는데 어차피 리턴값으로 예외 던지면 되니까 삭제
            string scoreHash = "";
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(userName + score.ToString() + Secret));
                var builder = new StringBuilder(hash.Length * 2);

                foreach (var b in hash)
                    builder.Append(b.ToString("X2"));

                scoreHash = builder.ToString().ToLower();
            }

            var data = new Dictionary<string, string>()
            {
                { "token", Token },
                { "name", userName },
                { "score", score.ToString() },
                { "hash", scoreHash }
            };

            if (memberSerial > 0)
            {
                //트루가 1 폴스가 0인데 음 난 모르겠다
                data.Add("member_srl", memberSerial.ToString());
                if (update)
                    data.Add("nchk", "1"); //Jump! 소스보고 고치자
            }

            var response = await client.PostAsync(scoreboardPostUrl, new FormUrlEncodedContent(data));
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            switch ((int)result["status"])
            {
                case -1:
                    throw new CookieeException("점수, 이름, 해시가 변조되었습니다. (해시 불일치)");
                case -2:
                    throw new CookieeException("토큰이 유효하지 않습니다.");
                case -3:
                    throw new CookieeException("새 점수가 이전 점수보다 작습니다.");
                case -4:
                    throw new CookieeException("토큰, 이름 또는 점수가 입력되지 않았습니다.");
            }

            return (int)result["rank"];
        }
        /// <summary>
        /// 점수를 추가합니다.
        /// </summary>
        /// <param name="userName">추가할 이름입니다.</param>
        /// <param name="score">추가할 점수입니다.</param>
        /// <param name="memberSerial"></param>
        /// <param name="update"></param>
        /// <returns>추가한 점수의 순위를 반환합니다.</returns>
        public int Add(string userName, long score, int memberSerial, bool update)
        {
            string scoreHash = "";
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(userName + score.ToString() + Secret));
                var builder = new StringBuilder(hash.Length * 2);

                foreach (var b in hash)
                    builder.Append(b.ToString("X2"));

                scoreHash = builder.ToString().ToLower();
            }

            using (var webClient = new WebClient())
            {
                var data = new NameValueCollection
                {
                    { "token", Token },
                    { "name", userName },
                    { "score", score.ToString() },
                    { "hash", scoreHash }
                };

                if (memberSerial > 0)
                {
                    //트루가 1 폴스가 0인데 음 난 모르겠다
                    data.Add("member_srl", memberSerial.ToString());
                    if (update)
                        data.Add("nchk", "1"); //Jump! 소스보고 고치자
                }

                var response = webClient.UploadValues(scoreboardPostUrl, data);
                var result = JObject.Parse(Encoding.Default.GetString(response));

                switch ((int)result["status"])
                {
                    case -1:
                        throw new CookieeException("점수, 이름, 해시가 변조되었습니다. (해시 불일치)");
                    case -2:
                        throw new CookieeException("토큰이 유효하지 않습니다.");
                    case -3:
                        throw new CookieeException("새 점수가 이전 점수보다 작습니다.");
                    case -4:
                        throw new CookieeException("토큰, 이름 또는 점수가 입력되지 않았습니다.");
                }

                return (int)result["rank"];
            }
        }
        /// <summary>
        /// 비동기로 점수를 최대 50개 가져옵니다.
        /// </summary>
        /// <returns>가져온 점수(최대 50개)를 배열로 반환합니다.</returns>
        public async Task<Score[]> GetAsync()
        {
            var result = JArray.Parse(JObject.Parse(await client.GetStringAsync($"{scoreboardGetUrl}/{Token}"))["list"].ToString());
            var scores = new Score[result.Count];
            for (int i = 0; i < result.Count; i++)
            {
                var parsed = JObject.Parse(result[i].ToString());
                scores[i] = new Score(int.Parse(parsed["rank"].ToString()), int.Parse(parsed["user_srl"].ToString()), parsed["name"].ToString(),
                    Convert.ToInt64(parsed["score"].ToString()), DateTime.ParseExact(parsed["time"].ToString(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            }

            return scores;
        }
        /// <summary>
        /// 비동기로 점수를 주어진 개수만큼 가져옵니다.
        /// </summary>
        /// <param name="count">가져올 점수의 개수입니다.</param>
        /// <returns>가져온 점수를 배열로 반환합니다.</returns>
        public async Task<Score[]> GetAsync(int count)
        {
            var result = JArray.Parse(JObject.Parse(await client.GetStringAsync($"{scoreboardGetUrl}/{Token}"))["list"].ToString()).Take(count).ToArray();
            var scores = new Score[result.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var parsed = JObject.Parse(result[i].ToString());
                scores[i] = new Score(int.Parse(parsed["rank"].ToString()), int.Parse(parsed["user_srl"].ToString()), parsed["name"].ToString(),
                    Convert.ToInt64(parsed["score"].ToString()), DateTime.ParseExact(parsed["time"].ToString(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            }

            return scores;
        }
        /// <summary>
        /// 점수를 최대 50개 가져옵니다.
        /// </summary>
        /// <returns>가져온 점수(최대 50개)를 배열로 반환합니다.</returns>
        public Score[] Get()
        {
            var request = (HttpWebRequest)WebRequest.Create(scoreboardGetUrl + "/" + Token);
            JArray result = null;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                result = JArray.Parse(JObject.Parse(reader.ReadToEnd())["list"].ToString());

            var scores = new Score[result.Count];
            for (int i = 0; i < result.Count; i++)
            {
                var parsed = JObject.Parse(result[i].ToString());
                scores[i] = new Score(int.Parse(parsed["rank"].ToString()), int.Parse(parsed["user_srl"].ToString()), parsed["name"].ToString(),
                    Convert.ToInt64(parsed["score"].ToString()), DateTime.ParseExact(parsed["time"].ToString(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            }

            return scores;
        }
        /// <summary>
        /// 점수를 주어진 개수만큼 가져옵니다.
        /// </summary>
        /// <param name="count">가져올 점수의 개수입니다.</param>
        /// <returns>가져온 점수를 배열로 반환합니다.</returns>
        public JObject Get(int count)
        {
            var request = (HttpWebRequest)WebRequest.Create(scoreboardGetUrl + $"/{Token}&count={count}");
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return JObject.Parse(reader.ReadToEnd());
        }
    }

    /// <summary>
    /// Cookiee 로그인 클래스입니다.
    /// </summary>
    public class Login
    {
        /// <summary>
        /// 해당 게임의 토큰입니다.
        /// </summary>
        public string Token { get; set; }
        private readonly HttpClient client = new HttpClient();
        private readonly string loginUrl = "https://www.cookiee.net/tools/login_normal";
        private readonly string socialLoginUrl = "https://www.cookiee.net/login_open.direct";
        private readonly string socialLoginCheckUrl = "https://www.cookiee.net/tools/login_social_check";
        private readonly string userStatusGetUrl = "https://www.cookiee.net/tools/user_status_get";

        /// <summary>
        /// 비동기로 일반 로그인을 진행합니다.
        /// </summary>
        /// <param name="id">로그인할 ID입니다.</param>
        /// <param name="password">로그인할 비밀번호입니다.</param>
        /// <returns>유저 SRL을 반환합니다.</returns>
        public async Task<int> NormalLoginAsync(string id, string password)
        {
            var data = new Dictionary<string, string>()
            {
                { "uid", id },
                { "ups", password },
                { "tkn", Token }
            };

            var response = await client.PostAsync(loginUrl, new FormUrlEncodedContent(data));
            var temp = response.Content.ReadAsStringAsync();
            var result = Convert.ToInt32(await response.Content.ReadAsStringAsync());

            switch (result)
            {
                case -1:
                    throw new CookieeException("아이디 또는 비밀번호가 입력되지 않았습니다.");
                case -2:
                    throw new CookieeException("Request Method가 POST가 아닙니다.");
                case -3:
                    throw new CookieeException("아이디 또는 비밀번호가 일치하지 않습니다.");
                case -4:
                    throw new CookieeException("통신 방식이 Https가 아닙니다.");
                default:
                    if (result < 0)
                        throw new CookieeException("알 수 없는 오류입니다.");
                    else
                        return result;
            }
        }
        /// <summary>
        /// 일반 로그인을 진행합니다.
        /// </summary>
        /// <param name="id">로그인할 ID입니다.</param>
        /// <param name="password">로그인할 비밀번호입니다.</param>
        /// <returns>유저 SRL을 반환합니다.</returns>
        public int NormalLogin(string id, string password)
        {
            using (var webClient = new WebClient())
            {
                var data = new NameValueCollection
                {
                    { "uid", id },
                    { "ups", password },
                    { "tkn", Token }
                };

                var response = webClient.UploadValues(loginUrl, data);
                var result = Convert.ToInt32(Encoding.Default.GetString(response));

                switch (result)
                {
                    case -1:
                        throw new CookieeException("아이디 또는 비밀번호가 입력되지 않았습니다.");
                    case -2:
                        throw new CookieeException("Request Method가 POST가 아닙니다.");
                    case -3:
                        throw new CookieeException("아이디 또는 비밀번호가 일치하지 않습니다.");
                    case -4:
                        throw new CookieeException("통신 방식이 Https가 아닙니다.");
                    case -99:
                        throw new CookieeException("시스템이 점검 중입니다.");
                    default:
                        if (result < 0)
                            throw new CookieeException("알 수 없는 오류입니다.");
                        else
                            return result;
                }
            }
        }
        /// <summary>
        /// 비동기로 소셜 로그인을 진행합니다. 브라우저 창을 엽니다.
        /// </summary>
        /// <returns>유저 SRL을 반환합니다.</returns>
        public async Task<int> SocialLoginAsync()
        {
            string hash = Guid.NewGuid().ToString().Replace("-", "");
            System.Diagnostics.Process.Start($"{socialLoginUrl}?token={WebUtility.UrlEncode(Token)}&hash={hash}");
            var data = new Dictionary<string, string>()
                {
                    { "token", Token },
                    { "hs", hash }
                };

            int result = -2;
            for (int i = 0; i < 60000; i++)
            {
                var response = await client.PostAsync(socialLoginCheckUrl, new FormUrlEncodedContent(data));
                result = (int)JObject.Parse(await response.Content.ReadAsStringAsync())["user_srl"];
                Console.WriteLine(result);

                if (result >= 0)
                    break;

                await Task.Delay(500);
            }

            switch (result)
            {
                case -1:
                    throw new CookieeException("로그인 데이터가 존재하지 않습니다. (로그인되지 않음)");
                case -99:
                    throw new CookieeException("시스템이 점검 중입니다.");
                default:
                    if (result < 0)
                        throw new CookieeException("알 수 없는 오류입니다.");
                    else
                        return result;
            }
        }
        /// <summary>
        /// 비동기로 소셜 로그인을 진행합니다. 브라우저 창을 엽니다.
        /// </summary>
        /// <returns>유저 SRL을 반환합니다.</returns>
        public int SocialLogin()
        {
            string hash = WebUtility.UrlEncode(Guid.NewGuid().ToString().Replace("-", ""));
            System.Diagnostics.Process.Start($"{socialLoginUrl}?token={WebUtility.UrlEncode(Token)}&hash={hash}");
            using (var webClient = new WebClient())
            {
                var data = new NameValueCollection
                    {
                        { "token", Token },
                        { "hs", hash }
                    };

                int result = -2;
                for (int i = 0; i < 60000; i++)
                {
                    //??? JSON으로 결과 받아오는데 뭐지 API 설명이랑 다르자너ㅠ
                    //다른 건 JSON 통채로 리턴하는데 굳이 그럴 필요가 없어서 웬만하면 꼭 필요한 정보 하나만 리턴하도록 바꾸는 중
                    var response = webClient.UploadValues(socialLoginCheckUrl, data);
                    result = (int)JObject.Parse(Encoding.Default.GetString(response))["user_srl"];
                    Console.WriteLine(result);

                    if (result >= 0)
                        break;

                    System.Threading.Thread.Sleep(500);
                }

                switch (result)
                {
                    case -1:
                        throw new CookieeException("로그인 데이터가 존재하지 않습니다. (로그인되지 않음)");
                    case -99:
                        throw new CookieeException("시스템이 점검 중입니다.");
                    default:
                        if (result < 0)
                            throw new CookieeException("알 수 없는 오류입니다.");
                        else
                            return result;
                }
            }
        }
        /// <summary>
        /// 유저 SRL을 통해 비동기로 유저 정보를 받아옵니다.
        /// </summary>
        /// <param name="memberSerial">정보를 받아올 유저의 SRL 값입니다.</param>
        /// <returns>유저 SRL과 일치하는 유저를 반환합니다.</returns>
        public async Task<CookieeUser> GetUserAsync(int memberSerial)
        {
            var data = new Dictionary<string, string>()
            {
                { "mr", memberSerial.ToString() },
                { "token", Token }
            };

            var response = await client.PostAsync(userStatusGetUrl, new FormUrlEncodedContent(data));
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            return new CookieeUser(Convert.ToInt32(result["member_srl"]), result["user_id"].ToString(), result["nick_name"].ToString(), result["profile_image"].ToString());
        }
        /// <summary>
        /// 유저 SRL을 통해 유저 정보를 받아옵니다.
        /// </summary>
        /// <param name="memberSerial">정보를 받아올 유저의 SRL 값입니다.</param>
        /// <returns>유저 SRL과 일치하는 유저를 반환합니다.</returns>
        public CookieeUser GetUser(int memberSerial)
        {
            using (var webClient = new WebClient())
            {
                var data = new NameValueCollection
                    {
                        { "mr", memberSerial.ToString() },
                        { "token", Token }
                    };

                var response = webClient.UploadValues(userStatusGetUrl, data);
                var result = JObject.Parse(Encoding.Default.GetString(response));

                return new CookieeUser(Convert.ToInt32(result["member_srl"]), result["user_id"].ToString(), result["nick_name"].ToString(), result["profile_image"].ToString());
            }
        }
    }

    /// <summary>
    /// Cookiee 게임 관리 클래스입니다.
    /// </summary>
    public class GameData
    {
        /// <summary>
        /// 해당 게임의 토큰입니다.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 해당 게임의 시크릿입니다.
        /// </summary>
        public string Secret { get; set; }
        private readonly HttpClient client = new HttpClient();
        private readonly string gameDataSaveUrl = "https://www.cookiee.net/tools/game_data_save";
        private readonly string gameDataLoadUrl = "https://www.cookiee.net/tools/game_data_load";

        /// <summary>
        /// 데이터를 비동기로 해당 유저에게 저장합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 저장할 유저의 SRL입니다.</param>
        /// <param name="data">저장할 데이터입니다. Dictionary의 키가 데이터의 이름, 값이 데이터의 값입니다.</param>
        /// <param name="isJson">데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>저장이 성공했는지 여부를 반환합니다.</returns>
        public async Task<bool> SaveAsync(int userSerial, Dictionary<string, object> data, bool isJson = true)
        {
            //API 설명이랑 다른 게 많네
            //먼저 해시는 base64 인코딩 전 데이터 + 시크릿이랬는데 실제론 sha1(base64 인코딩 후 데이터 + 시크릿)이었다
            //데이터는 넣을 데이터만 보내는 게 아니라 모든 데이터를 보내는 거였음

            //(모든 데이터 -> JSON 인코딩 -> base64 인코딩) -> + 시크릿 sha1 해싱
            /*if (allData.Count == 0)
                allData = Load(userSerial);*/

            //와진짜 정보 보낼때 데이터를 UrlEncode()해서 보냈는데 그거!!!!!떄문에!!!!!!안된거였어!!!!!!!와진자개빡쳐
            string[] jsonTemp = new string[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                var indexed = data.ElementAt(i);
                jsonTemp[i] = $"\"{indexed.Key}\": " + (Regex.IsMatch(indexed.Value.ToString(), @"^\d+$") ? indexed.Value : $"\"{indexed.Value}\"");
            }

            string realData = Convert.ToBase64String(Encoding.UTF8.GetBytes("{ " + string.Join(", ", jsonTemp) + " }"));
            string hash = "";
            using (var sha1 = new SHA1Managed())
            {
                var tempHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(realData + Secret));
                var builder = new StringBuilder(hash.Length * 2);

                foreach (var b in tempHash)
                    builder.Append(b.ToString("X2"));

                hash = builder.ToString().ToLower();
            }
            Console.WriteLine(realData);
            Console.WriteLine(hash);
            Console.WriteLine(WebUtility.UrlEncode(realData));

            var temp = new Dictionary<string, string>
            {
                { "token", Token },
                { "srl", userSerial.ToString() },
                { "ds", realData },
                { "hs", hash },
                { "ct", isJson ? "json" : "xml" }
            };

            var response = await client.PostAsync(gameDataSaveUrl, new FormUrlEncodedContent(temp));
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            var result = Convert.ToInt32(await response.Content.ReadAsStringAsync());

            return (result > 0) ? true : false;
        }
        /// <summary>
        /// 데이터를 해당 유저에게 저장합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 저장할 유저의 SRL입니다.</param>
        /// <param name="data">저장할 데이터입니다. Dictionary의 키가 데이터의 이름, 값이 데이터의 값입니다.</param>
        /// <param name="isJson">데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>저장이 성공했는지 여부를 반환합니다.</returns>
        public bool Save(int userSerial, Dictionary<string, object> data, bool isJson = true)
        {
            string[] jsonTemp = new string[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                var indexed = data.ElementAt(i);
                jsonTemp[i] = $"\"{indexed.Key}\": " + (Regex.IsMatch(indexed.Value.ToString(), @"^\d+$") ? indexed.Value : $"\"{indexed.Value}\"");
            }

            string realData = Convert.ToBase64String(Encoding.UTF8.GetBytes("{ " + string.Join(", ", jsonTemp) + " }"));
            string hash = "";
            using (var sha1 = new SHA1Managed())
            {
                var tempHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(realData + Secret));
                var builder = new StringBuilder(hash.Length * 2);

                foreach (var b in tempHash)
                    builder.Append(b.ToString("X2"));

                hash = builder.ToString().ToLower();
            }
            Console.WriteLine(realData);
            Console.WriteLine(hash);
            Console.WriteLine(WebUtility.UrlEncode(realData));

            using (var webClient = new WebClient())
            {
                var temp = new NameValueCollection
                {
                    { "token", Token },
                    { "srl", userSerial.ToString() },
                    { "ds", realData },
                    { "hs", hash },
                    { "ct", isJson ? "json" : "xml" }
                };

                var response = webClient.UploadValues(gameDataSaveUrl, temp);
                Console.WriteLine(Encoding.Default.GetString(response));
                var result = Convert.ToInt32(Encoding.Default.GetString(response));

                return (result > 0) ? true : false;
            }
        }
        /// <summary>
        /// 해당 유저의 데이터를 비동기로 불러옵니다.
        /// </summary>
        /// <param name="userSerial">데이터를 불러올 유저의 SRL 값입니다.</param>
        /// <returns>불러온 데이터를 반환합니다. Dictionary의 키가 데이터의 이름, 값이 데이터의 값입니다.</returns>
        public async Task<Dictionary<string, object>> LoadAsync(int userSerial)
        {
            var temp = new Dictionary<string, string>
            {
                { "token", Token },
                { "srl", userSerial.ToString() }
            };

            var response = await client.PostAsync(gameDataLoadUrl, new FormUrlEncodedContent(temp));
            var result = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(await response.Content.ReadAsStringAsync())));
            var data = new Dictionary<string, object>();

            foreach (var element in result)
                data.Add(element.Key, element.Value.ToString());

            return data;
        }
        /// <summary>
        /// 해당 유저의 데이터를 불러옵니다.
        /// </summary>
        /// <param name="userSerial">데이터를 불러올 유저의 SRL 값입니다.</param>
        /// <returns>불러온 데이터를 반환합니다. Dictionary의 키가 데이터의 이름, 값이 데이터의 값입니다.</returns>
        public Dictionary<string, object> Load(int userSerial)
        {
            using (var webClient = new WebClient())
            {
                var temp = new NameValueCollection
                {
                    { "token", Token },
                    { "srl", userSerial.ToString() }
                };

                var response = webClient.UploadValues(gameDataLoadUrl, temp);
                var result = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.Default.GetString(response))));
                var data = new Dictionary<string, object>();

                foreach (var element in result)
                    data.Add(element.Key, element.Value.ToString());

                return data;
            }
        }
        /// <summary>
        /// 데이터 하나를 해당 유저에게 비동기로 추가합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 추가할 유저의 SRL 값입니다.</param>
        /// <param name="key">추가할 데이터의 이름입니다.</param>
        /// <param name="value">추가할 데이터의 값입니다.</param>
        /// <param name="isJson">데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>추가에 성공했는지 여부를 반환합니다.</returns>
        public async Task<bool> AddAsync(int userSerial, string key, object value, bool isJson = true)
        {
            var data = await LoadAsync(userSerial);
            data.Add(key, value);
            return await SaveAsync(userSerial, data, isJson);
        }
        /// <summary>
        /// 데이터 하나를 해당 유저에게 추가합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 추가할 유저의 SRL 값입니다.</param>
        /// <param name="key">추가할 데이터의 이름입니다.</param>
        /// <param name="value">추가할 데이터의 값입니다.</param>
        /// <param name="isJson">데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>추가에 성공했는지 여부를 반환합니다.</returns>
        public bool Add(int userSerial, string key, object value, bool isJson = true)
        {
            var data = Load(userSerial);
            data.Add(key, value);
            return Save(userSerial, data, isJson);
        }
        /// <summary>
        /// 해당 유저의 데이터 하나를 비동기로 삭제합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 삭제할 유저의 SRL 값입니다.</param>
        /// <param name="key">삭제할 데이터의 이름입니다.</param>
        /// <param name="isJson">삭제 완료된 데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>삭제에 성공했는지 여부를 반환합니다.</returns>
        public async Task<bool> DeleteAsync(int userSerial, string key, bool isJson = true)
        {
            var data = await LoadAsync(userSerial);
            data.Remove(key);
            return await SaveAsync(userSerial, data, isJson);
        }
        /// <summary>
        /// 해당 유저의 데이터 하나를 삭제합니다.
        /// </summary>
        /// <param name="userSerial">데이터를 삭제할 유저의 SRL 값입니다.</param>
        /// <param name="key">삭제할 데이터의 이름입니다.</param>
        /// <param name="isJson">삭제 완료된 데이터를 JSON으로 저장할 지 여부입니다. false이면 XML로 저장합니다. 기본값은 true입니다.</param>
        /// <returns>삭제에 성공했는지 여부를 반환합니다.</returns>
        public bool Delete(int userSerial, string key, bool isJson = true)
        {
            var data = Load(userSerial);
            data.Remove(key);
            return Save(userSerial, data, isJson);
        }
    }
}
