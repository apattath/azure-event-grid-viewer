using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System;

namespace viewer.Models;

public class EventGridPayloadObject
{
    /// <summary>
    /// Channel RegistrationId
    /// </summary>
    public Guid ChannelRegistrationId { get; set; }

    /// <summary>
    /// Recipient Id
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// Channel type, e.g. WhatsApp
    /// </summary>
    public string ChannelType { get; set; }

    /// <summary>
    /// Timestamp of the received message
    /// </summary>
    public DateTimeOffset ReceivedTimeStamp { get; set; }

    /// <summary>
    /// Attempts to convert this <see cref="EventGridPayloadObject"/> into the specified type.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="T"/> must be derived from <see cref="EventGridPayloadObject"/>.
    /// </remarks>
    /// <typeparam name="T">The type to convert to, must be derived from <see cref="EventGridPayloadObject"/>.</typeparam>
    /// <param name="convertedEventGridObject">This object as the specified type if the type is compatible, otherwise null.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryAs<T>([NotNullWhen(true)] out T? convertedEventGridObject)
        where T : EventGridPayloadObject
    {
        if (this is T derivedEventGridObject)
        {
            convertedEventGridObject = derivedEventGridObject;
            return true;
        }

        convertedEventGridObject = null;
        return false;
    }
}
