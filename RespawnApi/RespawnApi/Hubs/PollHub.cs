using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Polls;

namespace RespawnApi.Hubs
{
    /// <summary>
    /// SignalR hub for managing real-time poll updates and notifications.
    /// </summary>
    public class PollHub : Hub
    {
        /// <summary>
        /// Broadcasts an updated poll to all connected clients.
        /// </summary>
        /// <param name="poll">The updated poll data to send.</param>
        public async Task BroadcastPollUpdate(PollDto poll)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceivePollUpdate", poll);
            }
        }

        /// <summary>
        /// Notifies all clients that a poll has been deleted.
        /// </summary>
        /// <param name="pollId">The ID of the deleted poll.</param>
        public async Task BroadcastPollDelete(string pollId)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceivePollDelete", pollId);
            }
        }

        /// <summary>
        /// Broadcasts a vote submission event to all clients with the updated poll data.
        /// </summary>
        /// <param name="updatedPoll">The poll data after a vote has been submitted.</param>
        public async Task BroadcastVoteSubmitted(PollDto updatedPoll)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveVoteUpdate", updatedPoll);
            }
        }
    }
}