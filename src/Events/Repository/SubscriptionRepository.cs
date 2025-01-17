using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Altinn.Platform.Events.Configuration;
using Altinn.Platform.Events.Extensions;
using Altinn.Platform.Events.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;
using NpgsqlTypes;

namespace Altinn.Platform.Events.Repository
{
    /// <summary>
    /// Represents an implementation of <see cref="ISubscriptionRepository"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly string insertSubscriptionSql = "select * from events.insert_subscription(@sourcefilter, @subjectfilter, @typefilter, @consumer, @endpointurl, @createdby, @validated)";
        private readonly string getSubscriptionSql = "select * from events.getsubscription(@_id)";
        private readonly string deleteSubscription = "call events.deletesubscription(@_id)";
        private readonly string setValidSubscription = "call events.setvalidsubscription(@_id)";
        private readonly string getSubscriptionsExcludeOrgsSql = "select * from events.getsubscriptionsexcludeorgs(@source, @subject, @type)";
        private readonly string getSubscriptionByConsumerSql = "select * from events.getsubscriptionsbyconsumer(@_consumer, @_includeInvalid)";
        private readonly string connectionString;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepository"/> class.
        /// </summary>
        public SubscriptionRepository(IOptions<PostgreSQLSettings> postgresSettings, ILogger<SubscriptionRepository> logger)
        {
            connectionString =
                string.Format(postgresSettings.Value.ConnectionString, postgresSettings.Value.EventsDbPwd);

            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Subscription> CreateSubscription(Subscription eventsSubscription)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(insertSubscriptionSql, conn);
                pgcom.Parameters.AddWithValue("sourcefilter", eventsSubscription.SourceFilter.AbsoluteUri);

                if (eventsSubscription.SubjectFilter != null)
                {
                    pgcom.Parameters.AddWithValue("subjectfilter", eventsSubscription.SubjectFilter);
                }
                else
                {
                    pgcom.Parameters.AddWithValue("subjectfilter", DBNull.Value);
                }

                if (eventsSubscription.TypeFilter != null)
                {
                    pgcom.Parameters.AddWithValue("typefilter", eventsSubscription.TypeFilter);
                }
                else
                {
                    pgcom.Parameters.AddWithValue("typefilter", DBNull.Value);
                }

                pgcom.Parameters.AddWithValue("consumer", eventsSubscription.Consumer);
                pgcom.Parameters.AddWithValue("endpointurl", eventsSubscription.EndPoint.AbsoluteUri);
                pgcom.Parameters.AddWithValue("createdby", eventsSubscription.CreatedBy);
                pgcom.Parameters.AddWithValue("validated", false);

                await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync();
                await reader.ReadAsync();
                return GetSubscription(reader);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // CreateSubscription // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteSubscription(int id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(deleteSubscription, conn);
                pgcom.Parameters.AddWithValue("_id", id);

                await pgcom.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // DeleteSubscription // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SetValidSubscription(int id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(setValidSubscription, conn);
                pgcom.Parameters.AddWithValue("_id", id);

                await pgcom.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // SetValidSubscription // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Subscription> GetSubscription(int id)
        {
            Subscription subscription = null;
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(getSubscriptionSql, conn);
                pgcom.Parameters.AddWithValue("_id", NpgsqlDbType.Integer, id);

                await using (NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        subscription = GetSubscription(reader);
                    }
                }

                return subscription;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // GetSubscription // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Subscription>> GetSubscriptionsExcludeOrg(string source, string subject, string type)
        {
            List<Subscription> searchResult = new List<Subscription>();
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(getSubscriptionsExcludeOrgsSql, conn);
                pgcom.Parameters.AddWithValue("source", NpgsqlDbType.Varchar, source);
                pgcom.Parameters.AddWithValue("subject", NpgsqlDbType.Varchar, subject);
                pgcom.Parameters.AddWithValue("type", NpgsqlDbType.Varchar, type);

                await using (NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        searchResult.Add(GetSubscription(reader));
                    }
                }

                return searchResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // GetSubscriptionsExcludeOrg // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Subscription>> GetSubscriptionsByConsumer(string consumer, bool includeInvalid)
        {
            List<Subscription> searchResult = new List<Subscription>();
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using NpgsqlCommand pgcom = new NpgsqlCommand(getSubscriptionByConsumerSql, conn);
                pgcom.Parameters.AddWithValue("_consumer", NpgsqlDbType.Varchar, consumer);
                pgcom.Parameters.AddWithValue("_includeInvalid", NpgsqlDbType.Boolean, includeInvalid);
                await using (NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        searchResult.Add(GetSubscription(reader));
                    }
                }

                return searchResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostgresRepository // GetSubscriptionByConsumer // Exception");
                throw;
            }
        }

        private static Subscription GetSubscription(NpgsqlDataReader reader)
        {
            Subscription subscription = new Subscription
            {
                Id = reader.GetValue<int>("id"),
                SourceFilter = new Uri(reader.GetValue<string>("sourcefilter")),
                SubjectFilter = reader.GetValue<string>("subjectfilter"),
                TypeFilter = reader.GetValue<string>("typefilter"),
                Consumer = reader.GetValue<string>("consumer"),
                EndPoint = new Uri(reader.GetValue<string>("endpointurl")),
                CreatedBy = reader.GetValue<string>("createdby"),
                Created = reader.GetValue<DateTime>("time").ToUniversalTime()
            };
            return subscription;
        }
    }
}
