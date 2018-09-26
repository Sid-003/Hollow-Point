using System.IO;
using System.Reflection;
using UnityEngine;

namespace HollowPoint
{
    public static class LoadAssets
    {
        public static AudioClip bulletSoundFX;
        public static AudioClip[] airStrikeSoundFX = new AudioClip[3];

        public static Texture2D gunSprite;
        public static void LoadBulletSounds()
        {
            int bulletCount = 0;
            int airsupportCount = 0;
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (res.EndsWith(".wav"))
                {
                    Modding.Logger.Log("[HOLLOW POINT] Found sound effect! Saving it.");
                    Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                    if(audioStream != null && res.Contains("support"))
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        airStrikeSoundFX[airsupportCount++] = WavUtility.ToAudioClip(buffer);
                    }
                    else if (audioStream != null)
                    {
                        byte[] buffer = new byte[audioStream.Length];
                        audioStream.Read(buffer, 0, buffer.Length);
                        audioStream.Dispose();
                        bulletSoundFX = WavUtility.ToAudioClip(buffer);
                    }
                } else if (res.EndsWith(".png"))
                {
                    using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                    {
                        if (s == null) continue;
                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        s.Dispose();
                        //Create texture from bytes 
                        gunSprite = new Texture2D(1, 1);
                        gunSprite.LoadImage(buffer);
                        gunSprite.Apply();
                        Modding.Logger.Log("[HOLLOW POINT] Created sprite from embedded image: " + res);
                    }
                }
            }
        }
        

    }
}