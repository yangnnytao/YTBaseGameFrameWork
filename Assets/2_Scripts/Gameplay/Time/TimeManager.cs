using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using YGZFrameWork;

/// <summary>
/// 用于时间的管理以及相关工具的处理
/// </summary>
public class TimeManager : ManagerMono<TimeManager>, IManagerInterface
{
    public static DateTime DateTime1970 = new DateTime(1970, 1, 1).ToLocalTime();

    public string address;

    #region 事件
    /// <summary> 过了一天 </summary>
    public static int EVENT_TIMEFLOW_DAY = 4;
    /// <summary> 小时 </summary>
    public static int EVENT_TIMEFLOW_HOUR = 3;
    /// <summary> 分钟 </summary>
    public static int EVENT_TIMEFLOW_MINUTE = 2;
    /// <summary> 秒 </summary>
    public static int EVENT_TIMEFLOW_SECOND = 1;
    #endregion

    public void Initialize()
    {
        this.GetSeverTime();//获取获取时间
    }
    public int onlineTime = 0;//服务器今天运行了多少秒

    float _saveTime;
    void Update()
    {
        if (!IsNetworkTime)
            return;

        int delta = (int)(Time.realtimeSinceStartup - _saveTime);
        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                onlineTime++;
                //DoEvent(EVENT_TIMEFLOW_SECOND);
                if (onlineTime % 60 == 0)
                {
                    //DoEvent(EVENT_TIMEFLOW_MINUTE, (onlineTime / 60) % 60);//分钟的流逝
                    if (onlineTime % 3600 == 0)
                    {
                        //DoEvent(EVENT_TIMEFLOW_HOUR, (onlineTime / 3600) % 24);//小时的流逝
                        if (onlineTime % 86400 == 0)
                        {
                            //DoEvent(EVENT_TIMEFLOW_DAY);//天的流逝
                        }
                    }
                }
            }
            _saveTime = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// 多少秒后今天结束
    /// </summary>
    /// <returns></returns>
    private int GetTodayEndTime()
    {
        DateTime now = LocalTime;
        return (int)(new DateTime(now.Year, now.Month, now.Day).AddDays(1) - now).TotalSeconds;
    }

    public float startScaledGameTime;
    private long networkTimestamp;//获取的网络时间戳 为0表示未获取

    /// <summary>
    /// 服务器时间毫秒数
    /// -1 表示获取失败
    /// </summary>
    public long ServerTime
    {
        get
        {
            if (networkTimestamp == 0)
                return -1;
            return networkTimestamp + (int)((Time.realtimeSinceStartup - this.startScaledGameTime) * 1000);
        }
    }

    public void GetSeverTime(UnityAction cb = null)
    {
        return;
        NetWorkManager.Instance.SendGetMessage(URL.TIMEURL, null, (string res) =>
        {
            try
            {
                if (!string.IsNullOrEmpty(res))
                {
                    //JSONObject data = JsonUtility.FromJson<JSONObject> (res);
                    //if (data != null && data.HasKey("data") && data["data"].IsObject && data["data"].HasKey("t"))
                    //{
                    //    networkTimestamp = long.Parse(data["data"]["t"]);
                    //    startScaledGameTime = Time.realtimeSinceStartup;
                    //    DateTime dt = DateTime1970.AddMilliseconds(networkTimestamp);
                    //    onlineTime = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
                    //    _saveTime = Time.realtimeSinceStartup;
                    //    if (cb != null)
                    //    {
                    //        cb.Invoke();
                    //    }
                    //}
                }
            }
            catch (Exception e)
            {
                Debug.LogError("获取时间戳报错:" + e);
            }
        });
    }
    /// <summary>
    /// 先取网络时间 无则取本地时间
    /// </summary>
    public DateTime LocalTime
    {
        get
        {
            if (IsNetworkTime)
                return Now;
            else
                return DateTime.Now;
        }
    }

    /// <summary>
    /// 获取当前网络时间(无网络报错）
    /// </summary>
    public DateTime Now
    {
        get
        {
            if (networkTimestamp == 0)
            {
                Debug.LogError("未同步网络时间却想要取得网络时间！");
                return DateTime.Now;
            }
            DateTime dt = DateTime1970.AddMilliseconds(ServerTime);
            return dt;
        }
    }

    public bool IsNetworkTime
    {
        get
        {
            return networkTimestamp != 0;
        }
    }

    public static string GetTimeString(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string GetTimeStringHMS(DateTime dt)
    {
        return dt.ToString("mm:ss");
    }

    public static string Countdown(int value)
    {
        int minute = value / 60;
        int second = value % 60;
        return (minute < 10 ? "0" + minute : "" + minute) + ":" + (second < 10 ? "0" + second : "" + second);
    }
    public DateTime GetFormatDate(string timeString)
    {
        try
        {
            DateTime dt = DateTime.ParseExact(timeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            return dt;
        }
        catch (Exception e)
        {
            Debug.LogError("timeString:" + timeString + "--->" + e);
        }
        return LocalTime;
    }

    /// <summary>
    /// 时间戳转换成DateTime 1970-01-01到现在的毫秒数
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public static DateTime TicksToDate(long timeStamp)
    {
        return DateTime1970.AddMilliseconds(timeStamp);
    }

    /// <summary>
    /// 获取从 1970-01-01 到现在的毫秒数。
    /// </summary>
    /// <returns></returns>
    public static long GetTimeStamp()
    {
        return GetTimeStamp(TimeManager.Instance.LocalTime);
    }

    /// <summary>
    /// 计算 1970-01-01 到指定 <see cref="DateTime"/> 的毫秒数。
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="allDay">是否从0点开始计算</param>
    /// <returns></returns>
    public static long GetTimeStamp(DateTime dateTime, bool allDay = false)
    {
        if (dateTime == null)
            dateTime = TimeManager.Instance.LocalTime;
        long r = (long)(dateTime.ToLocalTime() - DateTime1970).TotalMilliseconds;
        if (allDay)
        {
            r = (long)(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day) - DateTime1970).TotalMilliseconds;
        }
        Debug.Log(string.Format("GetTimeStamp: dtTime:{0},r:{1}", dateTime.ToString(), r));
        return r;
    }

    /// <summary>
    /// 两个时间戳之间的秒数 
    /// </summary>
    /// <param name="before"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    public long GetSecondBetweenTimeStamp(long before, long now = 0)
    {
        if (now == 0)
        {
            if (TimeManager.Instance.IsNetworkTime)
            {
                now = ServerTime;
            }
            else
            {
                Debug.LogWarning("使用本地时间");
                now = TimeManager.GetTimeStamp();
            }
        }
        return (now - before) / 1000;
    }
    /// <summary>
    /// 判断是否是新的一天 
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="network">是否必须联网</param>
    /// <returns></returns>
    public bool IsNewDay(DateTime dateTime, bool network = true)
    {
        if (network && !IsNetworkTime)
            return false;
        return dateTime.DayOfYear != TimeManager.Instance.LocalTime.DayOfYear;
    }

    public bool IsNewDay(long timeStamp, bool network = true)
    {
        return IsNewDay(TicksToDate(timeStamp), network);
    }

    public static string GetBeiJingTime(DateTime dt)
    {
        if (dt == null)
            dt = TimeManager.Instance.LocalTime;
        return GetTimeString(TimeZoneInfo.ConvertTimeToUtc(dt).AddHours(8));
    }

    public void RegisterMsg()
    {
        // 当前无消息需要注册
    }

    public void ClearData()
    {
        // 当前无数据需要清空
    }
}
