// Hubs/PollHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using RespawnApi.Application.DTOs.Polls; // Pro PollDto

namespace RespawnApi.Hubs
{
    // Tento Hub bude sloužit k odesílání aktualizací anket klientům.
    // Prozatím nebude mít žádné metody volatelné klienty,
    // pouze metody, které bude volat server pro odeslání zpráv.
    public class PollHub : Hub
    {
        // Metoda, kterou může server volat k odeslání aktualizované ankety všem klientům.
        // Klienti budou naslouchat na událost "ReceivePollUpdate".
        public async Task BroadcastPollUpdate(PollDto poll)
        {
            if (Clients != null) // Kontrola pro jistotu
            {
                await Clients.All.SendAsync("ReceivePollUpdate", poll);
            }
        }

        // Metoda, kterou může server volat k odeslání informace o smazané anketě.
        // Klienti budou naslouchat na událost "ReceivePollDelete".
        public async Task BroadcastPollDelete(string pollId)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceivePollDelete", pollId);
            }
        }

        // Metoda pro odeslání informace, že hlas byl úspěšně zaznamenán a anketa byla aktualizována
        // (např. pro aktualizaci počtu hlasů a userVotedOptionIds u ostatních klientů)
        public async Task BroadcastVoteSubmitted(PollDto updatedPoll)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveVoteUpdate", updatedPoll);
            }
        }


        // Příklad, jak by mohlo vypadat připojení ke skupině pro konkrétní anketu (pokročilejší)
        // public async Task JoinPollGroup(string pollId)
        // {
        //     await Groups.AddToGroupAsync(Context.ConnectionId, $"poll-{pollId}");
        // }

        // public async Task LeavePollGroup(string pollId)
        // {
        //     await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll-{pollId}");
        // }
    }
}
