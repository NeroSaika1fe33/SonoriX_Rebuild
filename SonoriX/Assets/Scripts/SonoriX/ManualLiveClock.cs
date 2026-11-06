using SonoriX.Runtime;

namespace SonoriX
{
    //测试同步时间信息
    public sealed class ManualLiveClock : ILiveClock
    {
        private float currentTime;
        private float speed = 1f;
        public float CurrentTime => currentTime;

        public ManualLiveClock(float speed)
        {
            this.speed = speed;
        }

        public void Start(float startTime)
        {
            currentTime = startTime;
        }

        public void Stop()
        {
        }

        public void Tick(float deltaTime)
        {
            currentTime += deltaTime * speed;
        }
    }
}