using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Xna.Framework;

public enum NetRole { None, Host, Client }

// UDP-based LAN multiplayer with 4-digit code discovery.
//
// Flow:
//   Everyone in lobby broadcasts their code every 0.5s on DISC_PORT (11001).
//   When you call TryJoin(code), we start filtering incoming broadcasts for that code.
//   When we find the host's broadcast → send hello to their game port (PORT 11000).
//   Host's game socket receives hello → becomes Host, sends ack → client Connected.
//
public class GameNet : IDisposable
{
    public const int PORT      = 11000;  // game traffic
    public const int DISC_PORT = 11001;  // discovery broadcasts

    public NetRole Role      { get; private set; } = NetRole.None;
    public bool    Connected { get; private set; } = false;
    public bool    IsSeeking => _joinCode >= 0;
    public string  LocalIP   { get; private set; } = "?";

    // ── Sockets ────────────────────────────────────────────────────────────
    UdpClient  _udp;   // game traffic, bound to PORT
    UdpClient  _disc;  // discovery, bound to DISC_PORT

    IPEndPoint _remoteEP;
    IPEndPoint _anyEP = new IPEndPoint(IPAddress.Any, 0);

    IPAddress  _localAddr;

    // ── State ──────────────────────────────────────────────────────────────
    int    _myCode   = 0;
    int    _joinCode = -1;   // code user wants to join; -1 = not set

    PlayerInput _lastRemoteInput;
    byte        _localSeq        = 0;
    byte        _remoteSeq       = 0;
    byte        _localJumpSeq    = 0;   // steigt bei jedem neuen lokalen Jump
    byte        _lastRemoteJumpSeq = 0; // letzter verarbeiteter Jump-Seq des Gegners

    float _broadcastTimer = 0f;
    float _helloTimer     = 0f;

    bool   _hasPendingSync  = false;
    byte[] _pendingSyncData = new byte[40];

    bool    _hasPendingHit    = false;
    int     _pendingHitTarget = 0;
    Vector2 _pendingHitVel;
    float   _pendingHitDmg;
    float   _pendingHitHistun;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public void OpenLobby(int myCode)
    {
        _myCode    = myCode;
        _localAddr = IPAddress.Parse(GetLocalIP());
        LocalIP    = _localAddr.ToString();

        // Game socket — bound to PORT so others can connect
        _udp = new UdpClient(PORT);
        _udp.Client.Blocking = false;

        // Discovery socket — bound to DISC_PORT to receive broadcasts
        _disc = new UdpClient(DISC_PORT);
        _disc.EnableBroadcast    = true;
        _disc.Client.Blocking    = false;
    }

    // Called when the user types a code and presses Enter.
    public void TryJoin(int code)
    {
        if (_joinCode >= 0) return;   // ignore if already seeking
        _joinCode = code;
    }

    public void Dispose()
    {
        _udp?.Close();  _udp  = null;
        _disc?.Close(); _disc = null;
        Connected = false;
        Role      = NetRole.None;
        _joinCode = -1;
    }

    // ── Per-frame update ───────────────────────────────────────────────────

    public void Update(float dt)
    {
        if (_disc == null) return;

        // Broadcast our own code every 0.5 s (so others can find us as host)
        _broadcastTimer -= dt;
        if (_broadcastTimer <= 0f)
        {
            BroadcastCode();
            _broadcastTimer = 0.5f;
        }

        // If seeking: poll discovery socket for a host matching our code
        if (!Connected && _joinCode >= 0)
            PollDiscovery();

        // Retry hello to host if we found them but haven't received ack yet
        if (!Connected && Role == NetRole.Client && _remoteEP != null)
        {
            _helloTimer -= dt;
            if (_helloTimer <= 0f) { SendHello(); _helloTimer = 0.5f; }
        }
    }

    public void SendHit(int targetId, Vector2 vel, float dmg, float hitstun)
    {
        if (_udp == null || _remoteEP == null || !Connected) return;
        byte[] pkt = new byte[19];
        pkt[0] = 0x05; pkt[1] = _localSeq++; pkt[2] = (byte)targetId;
        Buffer.BlockCopy(BitConverter.GetBytes(vel.X),   0, pkt,  3, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(vel.Y),   0, pkt,  7, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(dmg),     0, pkt, 11, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(hitstun), 0, pkt, 15, 4);
        try { _udp.Send(pkt, 19, _remoteEP); } catch { }
    }

    public bool ConsumePendingHit(out int targetId, out Vector2 vel, out float dmg, out float hitstun)
    {
        if (_hasPendingHit)
        {
            targetId = _pendingHitTarget; vel = _pendingHitVel;
            dmg = _pendingHitDmg; hitstun = _pendingHitHistun;
            _hasPendingHit = false; return true;
        }
        targetId = 0; vel = Vector2.Zero; dmg = 0; hitstun = 0;
        return false;
    }

    public void SendInput(PlayerInput input)
    {
        if (_udp == null || _remoteEP == null || !Connected) return;
        if ((input.Raw & 0x08) != 0) _localJumpSeq++;  // neuer Jump → Seq erhöhen
        byte[] pkt = new byte[7];
        pkt[0] = 0x01; pkt[1] = _localSeq++; pkt[2] = input.Raw; pkt[3] = input.AimAngle;
        pkt[4] = _localJumpSeq;   // immer aktuelle Jump-Seq mitsenden
        try { _udp.Send(pkt, 7, _remoteEP); } catch { }
    }

    // Receives all pending game packets. Call every frame even in lobby
    // (needed to detect incoming client hellos → become Host).
    public PlayerInput ReceiveInput()
    {
        if (_udp == null) return _lastRemoteInput;

        while (_udp.Available > 0)
        {
            try
            {
                var ep   = _anyEP;
                byte[] d = _udp.Receive(ref ep);
                if (d.Length < 3) continue;
                byte type = d[0];
                byte seq  = d[1];

                // Client hello → we become Host
                if (type == 0x03 && !Connected)
                {
                    Role      = NetRole.Host;
                    _remoteEP = ep;
                    Connected = true;
                    SendLobbyAck();
                    continue;
                }

                // Host ack → we are Client, now Connected
                if (type == 0x04 && !Connected)
                {
                    Connected = true;
                    if (_remoteEP == null) _remoteEP = ep;
                    continue;
                }

                if (!Connected) continue;

                // Drop stale / out-of-order input packets
                int delta = (sbyte)(seq - _remoteSeq);
                if (delta <= 0) continue;
                _remoteSeq = seq;

                if (type == 0x01)
                {
                    byte raw     = d[2];
                    byte jumpSeq = d.Length >= 5 ? d[4] : (byte)0;
                    // Jump nur feuern wenn neue Seq → verhindert Doppelsprung durch Paket-Reihenfolge
                    if ((sbyte)(jumpSeq - _lastRemoteJumpSeq) > 0)
                    {
                        raw |= 0x08;   // Jump-Bit sicherstellen
                        _lastRemoteJumpSeq = jumpSeq;
                    }
                    else
                    {
                        raw &= 0xF7;   // Jump-Bit löschen (bereits verarbeitet)
                    }
                    _lastRemoteInput = new PlayerInput { Raw = raw, AimAngle = d.Length >= 4 ? d[3] : (byte)0 };
                }
                else if (type == 0x02 && d.Length >= 40)
                {
                    Buffer.BlockCopy(d, 0, _pendingSyncData, 0, 40);
                    _hasPendingSync = true;
                }
                else if (type == 0x05 && d.Length >= 19)
                {
                    _pendingHitTarget = d[2];
                    _pendingHitVel    = new Vector2(BitConverter.ToSingle(d, 3), BitConverter.ToSingle(d, 7));
                    _pendingHitDmg    = BitConverter.ToSingle(d, 11);
                    _pendingHitHistun = BitConverter.ToSingle(d, 15);
                    _hasPendingHit    = true;
                }
            }
            catch { break; }
        }

        return _lastRemoteInput;
    }

    public bool ConsumePendingSync(out byte[] data)
    {
        if (_hasPendingSync) { data = _pendingSyncData; _hasPendingSync = false; return true; }
        data = null;
        return false;
    }

    public void SendStateSync(Vector2 p1Pos, Vector2 p1Vel, float p1Dmg, int p1Stocks,
                              Vector2 p2Pos, Vector2 p2Vel, float p2Dmg, int p2Stocks,
                              int scoreP1, int scoreP2)
    {
        if (_udp == null || _remoteEP == null || !Connected) return;
        byte[] pkt = new byte[40];
        pkt[0] = 0x02; pkt[1] = _localSeq++;
        int i = 2;
        void WF(float v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, pkt, i, 4); i += 4; }
        WF(p1Pos.X); WF(p1Pos.Y); WF(p1Vel.X); WF(p1Vel.Y);
        pkt[i++] = (byte)Math.Min(255, (int)p1Dmg);
        pkt[i++] = (byte)p1Stocks;
        WF(p2Pos.X); WF(p2Pos.Y); WF(p2Vel.X); WF(p2Vel.Y);
        pkt[i++] = (byte)Math.Min(255, (int)p2Dmg);
        pkt[i++] = (byte)p2Stocks;
        pkt[i++] = (byte)scoreP1;
        pkt[i]   = (byte)scoreP2;
        try { _udp.Send(pkt, 40, _remoteEP); } catch { }
    }

    // ── Discovery helpers ──────────────────────────────────────────────────

    void BroadcastCode()
    {
        // Packet: [0x20, code_hi, code_lo]
        var pkt = new byte[] { 0x20, (byte)(_myCode >> 8), (byte)(_myCode & 0xFF) };
        try { _disc.Send(pkt, 3, new IPEndPoint(IPAddress.Broadcast, DISC_PORT)); } catch { }
    }

    void PollDiscovery()
    {
        if (_disc == null) return;
        while (_disc.Available > 0)
        {
            try
            {
                var ep   = _anyEP;
                byte[] d = _disc.Receive(ref ep);

                // Filter own broadcasts
                if (ep.Address.Equals(_localAddr)) continue;

                if (d.Length >= 3 && d[0] == 0x20)
                {
                    int code = (d[1] << 8) | d[2];
                    if (code == _joinCode)
                    {
                        // Found the host — send hello to their game port
                        _remoteEP = new IPEndPoint(ep.Address, PORT);
                        Role      = NetRole.Client;
                        SendHello();
                        _helloTimer = 0.5f;
                    }
                }
            }
            catch { break; }
        }
    }

    void SendHello()
    {
        if (_udp == null || _remoteEP == null) return;
        byte[] pkt = new byte[6]; pkt[0] = 0x03;
        try { _udp.Send(pkt, 6, _remoteEP); } catch { }
    }

    void SendLobbyAck()
    {
        if (_udp == null || _remoteEP == null) return;
        byte[] pkt = new byte[6]; pkt[0] = 0x04;
        try { _udp.Send(pkt, 6, _remoteEP); } catch { }
    }

    static string GetLocalIP()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
        }
        return "127.0.0.1";
    }
}
