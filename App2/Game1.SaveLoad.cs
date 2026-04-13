using System;
using System.IO;

public partial class Game1
{
    void LoadSave()
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BRAWLHAVEN", "save.dat");
            if (!File.Exists(path)) return;
            using var br = new BinaryReader(File.OpenRead(path));
            byte ver = br.ReadByte();
            if (ver < 2) return;
            _playerName      = br.ReadString();
            _mySkin          = Math.Clamp(br.ReadInt32(), 0, SKINS.Length - 1);
            _chalClaimed     = ver >= 4 ? br.ReadInt64() : br.ReadInt32();
            _statWins        = br.ReadInt32();
            _statHits        = br.ReadInt32();
            _statDmgDealt    = br.ReadInt32();
            _statBestStreak  = br.ReadInt32();
            _statPerfectWins = br.ReadInt32();
            _statFastWin     = br.ReadInt32();
            _statCurStreak   = br.ReadInt32();
            if (ver >= 3)
            {
                _coins      = br.ReadInt32();
                _ownedSkins = ver >= 5 ? br.ReadInt64() : (long)br.ReadInt32();
                _ownedSkins |= (1L << 0) | (1L << 8);
            }
            if (ver >= 4)
            {
                _chalActivated = br.ReadInt64();
                for (int i = 0; i < _chalBaselines.Length; i++)
                    _chalBaselines[i] = br.ReadInt32();
            }
            if (ver >= 6)
                _myCHPattern = Math.Clamp(br.ReadInt32(), 0, 80);
            if (ver >= 7)
            {
                _ownedCHPatternsLo = br.ReadInt64();
                _ownedCHPatternsHi = br.ReadInt32();
            }
            if (ver >= 8)
                _myArmSkin = Math.Clamp(br.ReadInt32(), 0, ARM_SKINS.Length - 1);
            if (ver >= 9)
                _showEyes = br.ReadBoolean();
            if (ver >= 10)
                _ownedArmSkins = br.ReadInt64();
            _ownedArmSkins |= (1L << 0);  // DEFAULT always free
            if (!ArmSkinOwned(_myArmSkin)) _myArmSkin = 0;
        }
        catch { }
    }

    void SaveGame()
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BRAWLHAVEN", "save.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var bw = new BinaryWriter(File.Create(path));
            bw.Write((byte)10);
            bw.Write(_playerName);
            bw.Write(_mySkin);
            bw.Write(_chalClaimed);
            bw.Write(_statWins);
            bw.Write(_statHits);
            bw.Write(_statDmgDealt);
            bw.Write(_statBestStreak);
            bw.Write(_statPerfectWins);
            bw.Write(_statFastWin);
            bw.Write(_statCurStreak);
            bw.Write(_coins);
            bw.Write(_ownedSkins);
            bw.Write(_chalActivated);
            foreach (int b in _chalBaselines) bw.Write(b);
            bw.Write(_myCHPattern);
            bw.Write(_ownedCHPatternsLo);
            bw.Write(_ownedCHPatternsHi);
            bw.Write(_myArmSkin);
            bw.Write(_showEyes);
            bw.Write(_ownedArmSkins);
        }
        catch { }
    }
}
