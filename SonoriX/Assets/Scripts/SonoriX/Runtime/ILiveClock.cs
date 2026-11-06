namespace SonoriX.Runtime
{
    public interface ILiveClock
    {
        float CurrentTime { get; }
        
        void Start(float startTime);
        void Stop();
        void Tick(float deltaTime);
    }
}