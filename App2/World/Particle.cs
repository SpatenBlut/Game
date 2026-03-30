using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Particle
{
    public Vector2 Pos,Vel; public Color Col; public float Life,MaxLife,Size;
    public bool Alive=>Life>0;
    public Particle(Vector2 p,Vector2 v,Color c,float l,float s){Pos=p;Vel=v;Col=c;Life=MaxLife=l;Size=s;}
    public void Update(float dt){Pos+=Vel*dt;Vel*=0.88f;Life-=dt;}
}
