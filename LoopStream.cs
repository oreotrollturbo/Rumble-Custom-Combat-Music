using NAudio.Wave;

public class LoopStream : WaveStream //All of this has been stolen from 
//https://mark-dot-net.blogspot.com/2009/10/looped-playback-in-net-with-naudio.html
{
    private readonly WaveStream sourceStream;

    /// <summary>
    /// Initializes a new instance of the LoopStream class.
    /// </summary>
    /// <param name="sourceStream">The stream to read from. The Read method of this stream should return 0 when it reaches the end.</param>
    public LoopStream(WaveStream sourceStream)
    {
        this.sourceStream = sourceStream;
        this.EnableLooping = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable looping.
    /// </summary>
    public bool EnableLooping { get; set; }

    /// <summary>
    /// Gets the wave format of the source stream.
    /// </summary>
    public override WaveFormat WaveFormat => sourceStream.WaveFormat;

    /// <summary>
    /// Gets the length of the source stream.
    /// </summary>
    public override long Length => sourceStream.Length;

    /// <summary>
    /// Gets or sets the position of the source stream.
    /// </summary>
    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }

    /// <summary>
    /// Reads data from the source stream, looping back to the start if necessary.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset to start writing at.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (sourceStream.Position == 0 || !EnableLooping)
                {
                    // Source stream is at the end, and looping is disabled.
                    break;
                }

                // Restart the source stream.
                sourceStream.Position = 0;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}
