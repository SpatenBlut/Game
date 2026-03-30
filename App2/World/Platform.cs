public enum PlatType { Main, Side, Small }

public class Platform
{
    public float X,Y,W,H; public PlatType Type;
    public Platform(float x,float y,float w,float h,PlatType t){X=x;Y=y;W=w;H=h;Type=t;}
}
