namespace VRageMath
{
    public class MyMovingAverage
    {
        private readonly Queue<float> m_queue = new Queue<float>();
        private readonly int m_windowSize;
        private int m_enqueueCounter;
        private readonly int m_enqueueCountToReset;

        public float Avg => m_queue.Count <= 0 ? 0.0f : (float)Sum / (float)m_queue.Count;

        public double Sum { get; private set; }

        public MyMovingAverage(int windowSize, int enqueueCountToReset = 1000)
        {
            m_windowSize = windowSize;
            m_enqueueCountToReset = enqueueCountToReset;
        }

        private void UpdateSum()
        {
            Sum = 0.0;
            foreach (double num in m_queue)
                Sum += num;
        }

        public void Enqueue(float value)
        {
            m_queue.Enqueue(value);
            ++m_enqueueCounter;
            if (m_enqueueCounter > m_enqueueCountToReset)
            {
                m_enqueueCounter = 0;
                UpdateSum();
            }
            else
                Sum += (double)value;

            while (m_queue.Count > m_windowSize)
                Sum -= (double)m_queue.Dequeue();
        }

        public void Reset()
        {
            Sum = 0.0;
            m_queue.Clear();
        }
    }
}
