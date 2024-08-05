// See https://aka.ms/new-console-template for more information

using System;
using System.Globalization;
using System.Text;

string filename = @"D:\WT.txt"; // record file path
bool close = false;

Analyze();
while (!close)
    switch (AskOperation())
    {
        case 1:
            Analyze(); break;
        case 0:
            close = true;
            Console.Out.WriteLine("Closing.");
            break;
    }


int AskOperation()
{
    Console.WriteLine("Close(0), Analyze(1) or Append? Enter a number or paste result directly:");
    string? Op = Console.ReadLine();
    if (int.TryParse(Op, out int res))
    {
        return res switch
        {
            0 => 0,
            1 => 1,
            _ => 0,
        };
    }
    if (string.IsNullOrEmpty(Op))
        return 0;
    Append(Op);
    return 2;
}

void Analyze()
{
    string[] content = File.ReadAllLines(filename).Skip(1).ToArray();
    Console.WriteLine("There are {0} matches recorded.", content.Length);
    List<Round> roundList = [];
    foreach (var rawRounds in content)
    {
        string[] rawRoundData = rawRounds.Split(',');
        Round round = new()
        {
            date = DateOnly.ParseExact(rawRoundData[0], "yyyy.M.d"),
            week = int.Parse(rawRoundData[1]),
            time = TimeOnly.Parse(rawRoundData[2]),
            map = rawRoundData[3],
            win = int.Parse(rawRoundData[4]) != 0,
            continuous = int.Parse(rawRoundData[5]) != 0,
            teamSize = int.TryParse(rawRoundData[6], out int teamSize) ? teamSize : null,
            nation = rawRoundData[7],
            tier = int.Parse(rawRoundData[8])
        };
        if (round.tier == 6 /*&& round.teamSize == null*/)
            roundList.Add(round);
    }
    int[] nationWin = new int[8]; // all SU DE EN JP US FR NATO
    int[] nationLost = new int[8];
    int[] timeWin = new int[25];  // 0 to 23 and all
    int[] timeLost = new int[25];
    double[] nationWR = new double[8];
    double[] timeWR = new double[25];
    Console.WriteLine("Records parsed. Last match at {0} {1}, {2}", roundList.Last().date, roundList.Last().time, roundList.Last().map);
    foreach (var round in roundList)
    {
        int nation = -1;
        switch (round.map)
        {
            case string su when su.Contains("库尔斯克"): nation = 1; break;
            case string de when de.Contains("莫兹多克"): nation = 2; break;
            case string en when en.Contains("西奈"): nation = 3; break;
            case string jp when jp.Contains("日本"): nation = 4; break;
            case string us when us.Contains("阿登"): nation = 5; break;
            case string fr when fr.Contains("马奇诺"): nation = 6; break;
            case string fr when fr.Contains("马其诺"): nation = 6; break;
            case string nato when nato.Contains("富尔达"): nation = 7; break;
            default: Console.WriteLine("Invalid map at round {0}", string.Join(',', round)); break;
        }
        if (nation == -1)
            break;
        if (round.win)
        {
            nationWin[nation]++;
            nationWin[0]++;
            timeWin[int.Parse(round.time.Hour.ToString())]++;
            timeWin[24]++;
        }
        else
        {
            nationLost[nation]++;
            nationLost[0]++;
            timeLost[int.Parse(round.time.Hour.ToString())]++;
            timeLost[24]++;
        }
    }

    Console.Out.WriteLine("WR per map:\nEnemy\tTotal\tWin\tLost\tWR");
    for (int i = 0; i < 8; i++)
    {
        if (nationWin[i] != 0)
            nationWR[i] = (double)nationWin[i] / (nationWin[i] + nationLost[i]);
        switch (i)
        {
            case 0: Console.Write("ALL\t"); break;
            case 1: Console.Write("SU\t"); break;
            case 2: Console.Write("DE\t"); break;
            case 3: Console.Write("EN\t"); break;
            case 4: Console.Write("JP\t"); break;
            case 5: Console.Write("US\t"); break;
            case 6: Console.Write("FR\t"); break;
            case 7: Console.Write("NATO\t"); break;
        }
        Console.WriteLine($"{{0}}\t{nationWin[i]}\t{nationLost[i]}\t{nationWR[i]:P}", nationWin[i] + nationLost[i]);
    }

    Console.Out.WriteLine("\nWR per hours:\nTime\tTotal\tWin\tLost\tWR");
    for (int i = 0; i < 25; i++)
    {
        if ((timeWin[i] + timeLost[i]) == 0) continue;
        if (timeWin[i] != 0)
            timeWR[i] = (double)timeWin[i] / (timeWin[i] + timeLost[i]);
        Console.Write(i == 24 ? "ALL\t" : $"{i}\t");
        Console.WriteLine($"{{0}}\t{timeWin[i]}\t{timeLost[i]}\t{timeWR[i]:P}", timeWin[i] + timeLost[i]);
    }
    Console.WriteLine("Output done.");
}

void Append(string result)
{
    string[] csvResult = ParseResult(result);

    string lastMatch = File.ReadLines(filename).Last();
    string[] rawRoundData = lastMatch.Split(',');
    Round lastRound = new()
    {
        date = DateOnly.ParseExact(rawRoundData[0], "yyyy.M.d"),
        week = int.Parse(rawRoundData[1]),
        time = TimeOnly.Parse(rawRoundData[2]),
        map = rawRoundData[3],
        win = int.Parse(rawRoundData[4]) != 0,
        continuous = int.Parse(rawRoundData[5]) != 0,
        teamSize = int.TryParse(rawRoundData[6], out int teamSize) ? teamSize : null,
        nation = rawRoundData[7],
        tier = int.Parse(rawRoundData[8])
    };
    var timeDelta = new TimeSpan(TimeOnly.Parse(csvResult[2]).Ticks - lastRound.time.Ticks);
    if (timeDelta.Ticks == 0)
    {
        Console.WriteLine("\n Same time, check input! \n");
        return;
    }
    bool continuous = true;
    if (timeDelta.TotalMinutes is >= 40 or <= 0)
        continuous = false;
    Console.WriteLine("\n Play continuous?（0 for No, 1 for Yes, null for Auto[{0}]):", continuous);

    if (int.TryParse(Console.ReadLine(), out int contInt))
    {
        continuous = contInt switch
        {
            0 => false,
            1 => true,
            _ => true
        };
    }

    Console.WriteLine("\n Players in squad (0 for unknown, 1 for no squad):");
    if (!int.TryParse(Console.ReadLine(), out int squadPlayers))
        squadPlayers = 0;


    Console.WriteLine("\n Nation (Default: {0}):", lastRound.nation);
    string? Nation = Console.ReadLine();
    if (string.IsNullOrEmpty(Nation)) { Nation = lastRound.nation; }

    Console.WriteLine("\n Assault level (Default: {0}):", lastRound.tier);
    if (!int.TryParse(Console.ReadLine(), out int Tier))
        Tier = lastRound.tier;

    string output = string.Join(',', csvResult) + "," + (continuous ? 1 : 0) + "," + squadPlayers + "," + Nation + "," + Tier + "\n"; // broken when all in Join()
    Console.WriteLine("\n" + output);
    File.AppendAllText(filename, output, Encoding.UTF8);
    Console.WriteLine("Append done. \n");
}

string[] ParseResult(string result)
{
    string[] data = result.Split(["】 ", " ? ","摧毁空中目标","活跃时长", "收益", "获胜奖励", "参与任务奖励", "游戏: ", "总计:"], StringSplitOptions.RemoveEmptyEntries);
    DateTime time = UnixTimeStampToDateTime(int.Parse(data[^2][..7], NumberStyles.HexNumber) + 1667997000); // 1667996400
    string map = data[2];
    bool win = data[0][..2] == "胜利";
    string[] output = [time.Date.ToString("yyyy.M.d"), time.DayOfWeek.ToString("D"), time.ToString("H:mm"), map, (win ? 1 : 0).ToString()];
    return output;
}

DateTime UnixTimeStampToDateTime(double UNIXTimeStamp)
{
    return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(UNIXTimeStamp).ToLocalTime();
}

struct Round
{
    public DateOnly date;
    public int week;
    public TimeOnly time;
    public string map;
    public bool win;
    public bool continuous;
    public int? teamSize;
    public string nation;
    public int tier;
}