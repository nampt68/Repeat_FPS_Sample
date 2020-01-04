using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;

public class GameStatistics
{

    public int rtt;

    private readonly int _no_frames = 128;

    Color fpsColor = new Color(0.5f, 0.0f, 0.2f);
    Color[] histColor = new Color[] { Color.green, Color.grey };

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_LastFrameTicks; // Ticks at start of last frame
    float m_FrameDurationMS;
    float[] m_FrameTimes;
    float[][] m_TicksPerFrame;
    long m_FrequencyMS;
    string m_GraphicsDeviceName;

    int m_LastWorldTick;

    private int frameCount = 0;
    private const int kAverageFrameCount = 64;

    char[] buf = new char[256];

    internal class RecorderEntry
    {
        public string name;
        public float time;
        public int count;
        public float avgTime;
        public float avgCount;
        public float accTime;
        public int accCount;
        public Recorder recorder;
    };

    RecorderEntry[] recordersList =
    {
        new RecorderEntry() { name="RenderLoop.Draw" },
        new RecorderEntry() { name="Shadows.Draw" },
        new RecorderEntry() { name="RenderLoopNewBatcher.Draw" },
        new RecorderEntry() { name="ShadowLoopNewBatcher.Draw" },
        new RecorderEntry() { name="RenderLoopDevice.Idle" },
        new RecorderEntry() { name="StaticBatchDraw.Count" },
    };


    [ConfigVar(Name = "show.fps", DefaultValue = "0", Description = "Set to value > 0 to see fps stats.")]
    public static ConfigVar showFPS;

    [ConfigVar(Name = "show.compactstats", DefaultValue = "1", Description = "Set to value > 0 to see compact stats.")]
    public static ConfigVar showCompactStats;

    public GameStatistics()
    {
        m_FrequencyMS = System.Diagnostics.Stopwatch.Frequency / 1000;
        //System.Diagnostics.Stopwatch.Frequency	10000000	long
        //m_FrequencyMS	10000	long 
        //초당 tick수를 나타낸다. 1000으로 나누면 ms당 tick 값이다. 그래서 이름이 m_FrequencyMS 이군.
        m_StopWatch = new System.Diagnostics.Stopwatch(); 
        //stopwatch를 생성한다.
        m_StopWatch.Start();
        //stopwatch를 시작한다.
        m_LastFrameTicks = m_StopWatch.ElapsedTicks; 
        // Gets the total elapsed time measured by the current instance, in timer ticks.
        m_FrameTimes = new float[_no_frames];
        //_no_frames = 128, 128개의 float 배열
        m_TicksPerFrame = new float[2][] { new float[_no_frames], new float[_no_frames] }; 
        //128개의 배열이 2개

        m_GraphicsDeviceName = SystemInfo.graphicsDeviceName;//		SystemInfo.graphicsDeviceName	"NVIDIA GeForce GTX 670M"	string

        for (int i = 0; i < recordersList.Length; i++) 
        {
            var sampler = Sampler.Get(recordersList[i].name);
            //Sampler 이름을 사전에 RecorderEntry[] recordersList에 정의해 뒀는데, 이것으로 Sampler를 얻어 온다. 
            
            if (sampler != null)
            {
                recordersList[i].recorder = sampler.GetRecorder();
            }
        }

        Console.AddCommand("show.profilers", CmdShowProfilers, "Show available profilers.");
    }

    void CmdShowProfilers(string[] args)
    {
        var names = new List<string>();
        Sampler.GetNames(names);
        //프로파일러의 갯수는 엄청나게 많다. 별도 문서 참조.
        string search = args.Length > 0 ? args[0].ToLower() : null;
        for(var i = 0; i < names.Count; i++)
        {
            if(search == null || names[i].ToLower().Contains(search))
                Console.Write(names[i]);
        }
    }



    void SnapTime()
    {
        long now = m_StopWatch.ElapsedTicks;//현재의 Tick
        long duration = now - m_LastFrameTicks;//이전 Tick과의 차이

        m_LastFrameTicks = now;//현재 Tick을 저장

        float d = (float)duration / m_FrequencyMS;
        //duration을 msec로 변환.
        m_FrameDurationMS = m_FrameDurationMS * 0.9f + 0.1f * d;//이전 snaptime으로 구한 시간에서 10%만 반영

        m_FrameTimes[Time.frameCount % m_FrameTimes.Length] = d;//Awake()이후 총 frame, 그것을 128로 나눈 나머지, 그것을 기준으로 m_FrameTimes에 msec를 저장. 실제 SnapTime간의 시간을 저장.
    }

    void RecordTimers()
    {
        int ticks = 0;
        if (GameWorld.s_Worlds.Count > 0)
        {
            var world = GameWorld.s_Worlds[0];

            // Number of ticks in world since last frame.
            ticks = world.worldTime.tick - m_LastWorldTick;
            int l = Time.frameCount % m_TicksPerFrame[0].Length;
            m_TicksPerFrame[0][l] = 1000.0f * world.worldTime.tickInterval * ticks;
            m_LastWorldTick = world.worldTime.tick;
            double lastTickTime = world.nextTickTime - world.worldTime.tickInterval;
            m_TicksPerFrame[1][l] = (float)(1000.0 * (Game.frameTime - lastTickTime));
        }

        // get timing & update average accumulators
        for (int i = 0; i < recordersList.Length; i++)
        {
            recordersList[i].time = recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;
            recordersList[i].count = recordersList[i].recorder.sampleBlockCount;
            recordersList[i].accTime += recordersList[i].time;
            recordersList[i].accCount += recordersList[i].count;
        }

        frameCount++;
        // time to time, update average values & reset accumulators
        if (frameCount >= kAverageFrameCount)
        {
            for (int i = 0; i < recordersList.Length; i++)
            {
                recordersList[i].avgTime = recordersList[i].accTime * (1.0f / kAverageFrameCount);
                recordersList[i].avgCount = recordersList[i].accCount * (1.0f / kAverageFrameCount);
                recordersList[i].accTime = 0.0f;
                recordersList[i].accCount = 0;

            }
            frameCount = 0;
        }
    }

    public void TickLateUpdate()
    {
        SnapTime();
        if(showCompactStats.IntValue > 0)
        {
            DrawCompactStats();
        }
        if (showFPS.IntValue > 0)
        {
            RecordTimers();
            DrawFPS();
        }
    }







    void DrawCompactStats()
    {
        DebugOverlay.AddQuadAbsolute(0, 0, 60, 14, '\0', new Vector4(1.0f, 1.0f, 1.0f, 0.2f));
        var c = StringFormatter.Write(ref buf, 0, "FPS:{0}", Mathf.RoundToInt(1000.0f / m_FrameDurationMS));
        DebugOverlay.WriteAbsolute(2, 2, 8.0f, buf, c);

        DebugOverlay.AddQuadAbsolute(62, 0, 60, 14, '\0', new Vector4(1.0f, 1.0f, 0.0f, 0.2f));
        if (rtt > 0)
            c = StringFormatter.Write(ref buf, 0, "RTT:{0}", rtt);
        else
            c = StringFormatter.Write(ref buf, 0, "RTT:---");
        DebugOverlay.WriteAbsolute(64, 2, 8.0f, buf, c);
    }

    void DrawFPS()
    {
        DebugOverlay.Write(0, 1, "{0} FPS ({1:##.##} ms)", Mathf.RoundToInt(1000.0f / m_FrameDurationMS), m_FrameDurationMS);
        float minDuration = float.MaxValue;
        float maxDuration = float.MinValue;
        float sum = 0;
        for (var i = 0; i < _no_frames; i++)
        {
            var frametime = m_FrameTimes[i];
            sum += frametime;
            if (frametime < minDuration) minDuration = frametime;
            if (frametime > maxDuration) maxDuration = frametime;
        }

        DebugOverlay.Write(Color.green, 0, 2, "{0:##.##}", minDuration);
        DebugOverlay.Write(Color.grey, 6, 2, "{0:##.##}", sum / _no_frames);
        DebugOverlay.Write(Color.red, 12, 2, "{0:##.##}", maxDuration);

        DebugOverlay.Write(0, 3, "Frame #: {0}", Time.frameCount);

        DebugOverlay.Write(0, 4, m_GraphicsDeviceName);


        int y = 6;
        for (int i = 0; i < recordersList.Length; i++)
            DebugOverlay.Write(0, y++, "{0:##.##}ms (*{1:##})  ({2:##.##}ms *{3:##})  {4}", recordersList[i].avgTime, recordersList[i].avgCount, recordersList[i].time, recordersList[i].count, recordersList[i].name);

        if (showFPS.IntValue < 3)
            return;

        y++;
        // Start at framecount+1 so the one we have just recorded will be the last
        DebugOverlay.DrawHist(0, y, 20, 2, m_FrameTimes, Time.frameCount + 1, fpsColor, 20.0f);
        DebugOverlay.DrawHist(0, y + 2, 20, 2, m_TicksPerFrame, Time.frameCount + 1, histColor, 3.0f * 16.0f);

        DebugOverlay.DrawGraph(0, y + 6, 40, 2, m_FrameTimes, Time.frameCount + 1, fpsColor, 20.0f);

        if (GameWorld.s_Worlds.Count > 0)
        {
            var world = GameWorld.s_Worlds[0];
            DebugOverlay.Write(0, y + 8, "Tick: {0:##.#}", 1000.0f * world.worldTime.tickInterval);
        }
    }

    
}
