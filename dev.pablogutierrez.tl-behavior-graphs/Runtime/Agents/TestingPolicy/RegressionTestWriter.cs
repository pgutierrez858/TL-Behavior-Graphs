using System.IO;
using Google.Protobuf;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine;

namespace TLTLUnity.Agents.Tests
{
    /// <summary>
    /// Responsible for writing regression test data to stream (typically a file stream).
    /// </summary>
    /// <seealso cref="RegressionTestRecorder"/>
    public class RegressionTestWriter
    {
        char m_Separator;
        /// <summary>
        /// How many lines have been written to stream.
        /// </summary>
        int m_WriteCount;
        StreamWriter m_Writer;
        /// <summary>
        /// List of metric names that are supported by this writer
        /// </summary>
        List<string> m_AvailableMetrics;
        /// <summary>
        /// Whether this writer has started writing to its associated stream.
        /// If this is true, no further metric names will be allowed in the header.
        /// </summary>
        bool m_StartedWriting;

        /// <summary>
        /// Dictionary containing the currently registered metrics. These are flushed 
        /// to a new line in the stream whenever the user calls the Record method, and 
        /// may only contain as keys the ones present in m_AvailableMetrics.
        /// </summary>
        Dictionary<string, float> m_BufferedMetrics;

        public int WriteCount
        {
            get { return m_WriteCount; }
        }

        /// <summary>
        /// Create a RegressionTestWriter that will write to the specified stream.
        /// The stream must support writes.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="separator">character to use as separator between metric values</param>
        public RegressionTestWriter(Stream stream, char separator = ',')
        {
            m_Writer = new StreamWriter(stream);
            m_AvailableMetrics = new List<string>();
            m_StartedWriting = false;
            m_Separator = separator;
            m_WriteCount = 0;
            m_BufferedMetrics = new Dictionary<string, float>();
        }

        /// <summary>
        /// Registers a new metric name to the writer, assuming writing has not started yet.
        /// </summary>
        /// <param name="metricName">Name of the metric to register.</param>
        internal void RegisterMetric(string metricName)
        {
            if (m_StartedWriting)
            {
                // Already started writing, no more metrics names allowed.
                return;
            }

            // add the new metric name if it was not already included in the headers.
            if (!m_AvailableMetrics.Contains(metricName))
            {
                m_AvailableMetrics.Add(metricName);
                m_BufferedMetrics.Add(metricName, float.NaN);
            }
        } // RegisterMetric



        /// <summary>
        /// Writes headers to stream. This only takes effect once as long as no other content has been written to the stream.
        /// </summary>
        internal void WriteHeaders()
        {
            if (m_Writer == null || m_StartedWriting)
            {
                // Already closed or already started writing to stream
                return;
            }

            m_Writer.WriteLine(string.Join(m_Separator, m_AvailableMetrics));
            // mark start of writing process.
            m_StartedWriting = true;
        } // WriteHeaders


        /// <summary>
        /// Write recorded metric to writer buffer.
        /// </summary>
        internal void Record(string metricName, float metricValue)
        {
            if (m_Writer == null)
            {
                Debug.LogWarning("Trying to record a metric while stream is closed.");
                // Already closed
                return;
            }

            if (!m_StartedWriting)
            {
                Debug.LogWarning("Trying to record a metric before writing headers line to stream.");
                return;
            }

            if (!m_BufferedMetrics.ContainsKey(metricName))
            {
                Debug.LogWarning($"Trying to record an unregistered metric: {metricName}.");
                return;
            }

            // add metric value to buffer
            m_BufferedMetrics[metricName] = metricValue;
        } // Record

        /// <summary>
        /// Flushes the current buffered metrics in a separate line to the stream, separated by m_Separator,
        /// increasing write count.
        /// </summary>
        internal void Flush()
        {
            for (int i = 0; i < m_AvailableMetrics.Count; i++)
            {
                string metric = m_AvailableMetrics[i];
                m_Writer.Write(m_BufferedMetrics[metric]);
                // reset value to NaN to prevent keeping previous values in new lines.
                m_BufferedMetrics[metric] = float.NaN;
                if (i != m_AvailableMetrics.Count - 1)
                {
                    m_Writer.Write(m_Separator);
                }
            }
            m_Writer.WriteLine();

            // Increment line counter
            m_WriteCount++;
        } // Flush

        /// <summary>
        /// Performs all clean-up necessary.
        /// </summary>
        public void Close()
        {
            if (m_Writer == null)
            {
                // Already closed
                return;
            }

            m_Writer.Close();
            m_Writer = null;
        } // Close
    }
}
