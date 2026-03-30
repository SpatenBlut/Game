public struct RectF
{
    public float X,Y,W,H;
    public float Left=>X; public float Right=>X+W;
    public float Top=>Y;  public float Bottom=>Y+H;
    public float Width=>W; public float Height=>H;
    public RectF(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
    public bool Intersects(RectF o)=>Left<o.Right&&Right>o.Left&&Top<o.Bottom&&Bottom>o.Top;
}
