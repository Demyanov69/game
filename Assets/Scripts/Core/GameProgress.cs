using UnityEngine;

namespace EchoIslands
{
    /// <summary>Small, versioned PlayerPrefs save: completed chapters, current level, checkpoint and inventory.</summary>
    public static class GameProgress
    {
        const string UnlockKey = "echo_unlocked";
        const string SaveKey = "echo_save_v2_level";
        public static int HighestUnlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey,1),1,5);
            set
            {
                int v = Mathf.Clamp(value,1,5);
                if(v>HighestUnlocked){PlayerPrefs.SetInt(UnlockKey,v);PlayerPrefs.Save();}
            }
        }
        public static int SavedLevel => Mathf.Clamp(PlayerPrefs.GetInt(SaveKey,1),1,5);
        public static bool HasSave => PlayerPrefs.HasKey(SaveKey);
        public static bool AllComplete => PlayerPrefs.GetInt("echo_complete",0)!=0;
        public static Vector3 SavedPosition => new Vector3(
            PlayerPrefs.GetFloat("echo_save_x",0f),
            PlayerPrefs.GetFloat("echo_save_y",1.3f),
            PlayerPrefs.GetFloat("echo_save_z",0f));
        public static WorldKind SavedWorld => PlayerPrefs.GetInt("echo_save_world",0)==1?WorldKind.Dark:WorldKind.Light;
        public static bool SavedStaff => PlayerPrefs.GetInt("echo_save_staff",0)==1;
        public static bool SavedClock => PlayerPrefs.GetInt("echo_save_clock",0)==1;
        public static bool SavedOrb => PlayerPrefs.GetInt("echo_save_orb",0)==1;
        public static float SavedLevel5Time => PlayerPrefs.GetFloat("echo_save_l5_time",600f);
        public static void Save(int level,Vector3 location,bool staff,bool clock,WorldKind world,bool orb=false,float level5Time=600f)
        {
            PlayerPrefs.SetInt(SaveKey,Mathf.Clamp(level,1,5));
            PlayerPrefs.SetFloat("echo_save_x",location.x);
            PlayerPrefs.SetFloat("echo_save_y",location.y);
            PlayerPrefs.SetFloat("echo_save_z",location.z);
            PlayerPrefs.SetInt("echo_save_staff",staff?1:0);
            PlayerPrefs.SetInt("echo_save_clock",clock?1:0);
            PlayerPrefs.SetInt("echo_save_world",world==WorldKind.Dark?1:0);
            PlayerPrefs.SetInt("echo_save_orb",orb?1:0);
            PlayerPrefs.SetFloat("echo_save_l5_time",level5Time);
            PlayerPrefs.Save();
        }
        public static void Unlock(int level){HighestUnlocked=level;}
        public static void Finish(){PlayerPrefs.SetInt("echo_complete",1);PlayerPrefs.Save();}
        public static void Reset()
        {
            foreach(var key in new [] { UnlockKey,SaveKey,"echo_save_x","echo_save_y","echo_save_z",
                "echo_save_staff","echo_save_clock","echo_save_world","echo_save_orb","echo_save_l5_time","echo_complete" })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
