using System;
using Microsoft.Data.SqlClient;

namespace ProbadorSQL_Pro
{
    // Clase para guardar los datos de la sesión (incluido el Puerto)
    public class SesionDatos
    {
        public string Host { get; set; } = "localhost";
        public string Puerto { get; set; } = "1433"; // Nuevo: Puerto por defecto
        public string Instancia { get; set; } = "";
        public string BaseDatos { get; set; } = "master";
        public bool WinAuth { get; set; } = false;
        public string Usuario { get; set; } = "sa";
        public string Password { get; set; } = "";
        public bool ConfiarCertificado { get; set; } = true;
    }

    class Program
    {
        // Variable para almacenar la última cadena generada o ingresada
        static string _ultimaCadenaUsada = "";

        static void Main(string[] args)
        {
            SesionDatos memoria = new SesionDatos();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==============================================");
            Console.WriteLine("   PRUEBA SQL (.NET 9) - COMPLETAMENTE DINÁMICO");
            Console.WriteLine("==============================================");
            Console.ResetColor();

            while (true)
            {
                // Estado visual de la opción de reintento
                string estadoReintento = string.IsNullOrEmpty(_ultimaCadenaUsada) ? "(Vacío)" : "(Listo)";
                ConsoleColor colorReintento = string.IsNullOrEmpty(_ultimaCadenaUsada) ? ConsoleColor.DarkGray : ConsoleColor.Green;

                Console.WriteLine("\nMenú Principal:");

                Console.ForegroundColor = colorReintento;
                Console.WriteLine($"1. Reintentar última conexión {estadoReintento}");
                Console.ResetColor();

                Console.WriteLine("2. Construir/Editar conexión (Paso a paso)");
                Console.WriteLine("3. Ingresar Cadena Completa (Manual)");
                Console.WriteLine("4. Limpiar memoria");
                Console.WriteLine("5. Salir");
                Console.Write("\n> Opción: ");

                var opcion = Console.ReadLine();
                string connectionString = "";

                switch (opcion)
                {
                    case "1":
                        if (string.IsNullOrEmpty(_ultimaCadenaUsada))
                        {
                            Console.WriteLine(">> No hay ninguna cadena guardada todavía.");
                            continue;
                        }
                        connectionString = _ultimaCadenaUsada;
                        Console.WriteLine("\n>> Reutilizando última cadena...");
                        break;

                    case "2":
                        connectionString = GenerarConMemoria(memoria);
                        break;

                    case "3":
                        Console.Write("\nPegue la cadena: ");
                        connectionString = Console.ReadLine() ?? "";
                        break;

                    case "4":
                        memoria = new SesionDatos();
                        _ultimaCadenaUsada = ""; // También limpiamos el historial
                        Console.WriteLine(">> Memoria y historial borrados.");
                        continue;

                    case "5":
                        return;

                    default:
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Guardamos la cadena para poder reusarla en la Opción 1
                    _ultimaCadenaUsada = connectionString;
                    EjecutarPrueba(connectionString);
                }
            }
        }

        static string GenerarConMemoria(SesionDatos datos)
        {
            var builder = new SqlConnectionStringBuilder();

            Console.WriteLine("\n--- Configuración de Servidor ---");

            // 1. Host
            Console.Write($"Host / IP [Actual: {datos.Host}]: ");
            string? input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) datos.Host = input;

            // 2. Puerto (NUEVO)
            Console.Write($"Puerto [Actual: {datos.Puerto}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) datos.Puerto = input;

            // 3. Instancia
            string textoInstancia = string.IsNullOrEmpty(datos.Instancia) ? "(Ninguna)" : datos.Instancia;
            Console.Write($"Instancia [Actual: {textoInstancia}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) datos.Instancia = input;
            if (input?.Trim() == "-") datos.Instancia = ""; // Opción para borrar instancia

            // --- Lógica de Construcción del DataSource ---
            // Formato estándar: Host\Instancia (si puerto es 1433)
            // Formato con puerto: Host,Puerto o Host\Instancia,Puerto

            string finalDataSource = datos.Host;

            // Si hay instancia, la agregamos
            if (!string.IsNullOrEmpty(datos.Instancia))
            {
                finalDataSource += "\\" + datos.Instancia;
            }

            // Si el puerto NO es el default (1433), lo agregamos con coma
            // SQL Server usa coma para el puerto, no dos puntos.
            if (datos.Puerto.Trim() != "1433")
            {
                finalDataSource += "," + datos.Puerto;
            }

            builder.DataSource = finalDataSource;
            // ---------------------------------------------

            Console.WriteLine("\n--- Credenciales y Base de Datos ---");

            // 4. Base de Datos
            Console.Write($"Base de Datos [Actual: {datos.BaseDatos}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) datos.BaseDatos = input;
            builder.InitialCatalog = datos.BaseDatos;

            // 5. Autenticación
            string authText = datos.WinAuth ? "Windows" : "SQL Server";
            Console.Write($"Modo Autenticación (W=Windows, S=SQL) [Actual: {authText}]: ");
            input = Console.ReadLine()?.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(input)) datos.WinAuth = (input == "W");

            if (datos.WinAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;

                Console.Write($"Usuario [Actual: {datos.Usuario}]: ");
                input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input)) datos.Usuario = input;
                builder.UserID = datos.Usuario;

                string maskPass = string.IsNullOrEmpty(datos.Password) ? "(Vacío)" : "******";
                Console.Write($"Contraseña [Actual: {maskPass}]: ");
                string? newPass = LeerPasswordOpcional();
                if (newPass != null) datos.Password = newPass;

                builder.Password = datos.Password;
            }

            // 6. Certificado
            string certText = datos.ConfiarCertificado ? "Sí" : "No";
            Console.Write($"¿Confiar Certificado (SSL)? (S/N) [Actual: {certText}]: ");
            input = Console.ReadLine()?.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(input)) datos.ConfiarCertificado = (input != "N");

            builder.Encrypt = true;
            builder.TrustServerCertificate = datos.ConfiarCertificado;
            builder.ConnectTimeout = 10; // Timeout corto para probar rápido

            string finalString = builder.ConnectionString;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[INFO] DataSource resultante: {builder.DataSource}");
            Console.WriteLine($"[INFO] Cadena completa: {finalString}");
            Console.ResetColor();

            return finalString;
        }

        static void EjecutarPrueba(string connString)
        {
            Console.WriteLine("\nConectando...");

            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✔ ¡CONEXIÓN EXITOSA!");
                    Console.ResetColor();

                    string sql = "SELECT @@SERVERNAME, DB_NAME(), local_net_address, local_tcp_port FROM sys.dm_exec_connections WHERE session_id = @@SPID";

                    // Nota: sys.dm_exec_connections requiere permisos, si falla usamos query simple
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                Console.WriteLine($"   Servidor SQL: {r[0]}");
                                Console.WriteLine($"   Base Datos:   {r[1]}");
                                Console.WriteLine($"   IP Destino:   {r["local_net_address"]}");
                                Console.WriteLine($"   Puerto TCP:   {r["local_tcp_port"]}");
                            }
                        }
                    }
                    catch
                    {
                        // Fallback si el usuario no tiene permisos para ver sys.dm_exec_connections
                        Console.WriteLine("   (Conexión establecida, pero usuario sin permisos para ver detalles de red)");
                    }
                }
                catch (SqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✘ ERROR SQL:");
                    Console.WriteLine($"   Nro: {ex.Number} | Mensaje: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✘ ERROR: {ex.Message}");
                }
                finally
                {
                    Console.ResetColor();
                    Console.WriteLine("Presiona una tecla para continuar...");
                    Console.ReadKey();
                }
            }
        }

        static string? LeerPasswordOpcional()
        {
            string pass = "";
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    if (pass.Length == 0) return null; // Retorna null para indicar "no cambiar"
                    return pass;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (pass.Length > 0)
                    {
                        pass = pass[0..^1];
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
            }
        }
    }
}