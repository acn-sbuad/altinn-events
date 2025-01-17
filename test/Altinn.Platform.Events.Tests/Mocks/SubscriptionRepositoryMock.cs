using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Altinn.Platform.Events.Models;
using Altinn.Platform.Events.Repository;

using Newtonsoft.Json;

namespace Altinn.Platform.Events.Tests.Mocks
{
    /// <summary>
    /// Represents a mock implementation of <see cref="ISubscriptionRepository"/>.
    /// </summary>
    public class SubscriptionRepositoryMock : ISubscriptionRepository
    {
        public Task<Subscription> CreateSubscription(Subscription eventsSubscription)
        {
            Random rnd = new Random();
            eventsSubscription.Id = rnd.Next(1, int.MaxValue);
            eventsSubscription.Created = DateTime.Now;
            return Task.FromResult(eventsSubscription);
        }

        public Task DeleteSubscription(int id)
        {
            return Task.CompletedTask;
        }

        public Task<Subscription> GetSubscription(int id)
        {
            return Task.FromResult(new Subscription() { Id = id, AlternativeSubjectFilter = "/organisation/950474084", CreatedBy = "/party/500700" });
        }

        public async Task<List<Subscription>> GetSubscriptionsByConsumer(string consumer, bool includeInvalid)
        {
            await Task.CompletedTask;

            string subscriptionsPath = Path.Combine(GetSubscriptionPath(), "1.json");
            if (File.Exists(subscriptionsPath))
            {
                string content = File.ReadAllText(subscriptionsPath);
                List<Subscription> allSubscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);

                if (consumer.EndsWith('%'))
                {
                    consumer = consumer.Replace("%", string.Empty);
                    return allSubscriptions.Where(s => s.Consumer.StartsWith(consumer)).ToList();
                }

                return allSubscriptions.Where(s => s.Consumer == consumer).ToList();
            }
            else
            {
                return new List<Subscription>();
            }
        }

        public Task<List<Subscription>> GetSubscriptionsExcludeOrg(string source, string subject, string type)
        {
            string subscriptionsPath = Path.Combine(GetSubscriptionPath(), "1.json");
            List<Subscription> subscriptions = null;
            if (File.Exists(subscriptionsPath))
            {
                string content = File.ReadAllText(subscriptionsPath);
                subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);
            }
            else
            {
                subscriptions = new List<Subscription>();
            }

            return Task.FromResult(subscriptions.Where(s =>
                                !s.Consumer.StartsWith("/org/") &&
                                s.SourceFilter.Equals(source) &&
                                subject.Equals(subject) &&
                                (string.IsNullOrEmpty(s.TypeFilter) || type.Equals(s.TypeFilter))).ToList());
        }

        public Task SetValidSubscription(int id)
        {
            return Task.CompletedTask;
        }

        private static string GetSubscriptionPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(EventsServiceMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "subscriptions");
        }
    }
}
