using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System;

namespace viewer.Models;

public class EventGridPayloadObject
{
    /// <summary>
    /// The time that the event is being created for Event Grid.
    /// </summary>
    /// <remarks>
    /// Changed Data type of EventTime as DateTime from DateTimeOffset because
    /// Event service uses dateTime and Conversion from DateTimeOffset is loosing datetime information
    /// and EventTime is getting populating with default vaule.
    /// </remarks>
    [Required]
    public DateTimeOffset? EventTime { get; init; }

    /// <summary>
    /// StableResourceId of Acs Resource
    /// </summary>
    [Required]
    public string AcsResourceId { get; set; }

    /// <summary>
    /// Channel type, e.g. WhatsApp
    /// </summary>
    public string ChannelType { get; set; }

    /// <summary>
    /// </summary>
    /// Sender Id
    public string From { get; set; }

    /// <summary>
    /// Recipient Id
    /// </summary>
    public string To { get; set; }

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
