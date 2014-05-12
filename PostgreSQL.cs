using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace BalanceChecker
{
	public static class PostgreSQL
	{
		public static NpgsqlConnection Get()
		{
			try
			{
				var connstring = String.Format("Server={0};Port={1};" +
						"User Id={2};Password={3};Database={4};",
						Settings.Default.Server, Settings.Default.Port, Settings.Default.UserId,
						Settings.Default.Password, Settings.Default.Database);

				NpgsqlConnection conn = new NpgsqlConnection(connstring);
				return conn;
			}
			catch (NpgsqlException ex)
			{
				Log.Write(ex.Message);
				return null;
			};
		}		
	}
}
