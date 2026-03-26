using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using OBS.Models;

namespace OBS.DataAccess
{
    public class StudentNoteRepository
    {
        public StudentNoteRepository()
        {
            DatabaseConnection.EnsureDatabase();
        }

        public List<StudentNote> GetNotesByStudent(string studentNumber)
        {
            var list = new List<StudentNote>();
            if (string.IsNullOrWhiteSpace(studentNumber)) return list;

            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            // En yeni not en üstte görünecek şekilde sıralanır (DESC)
            cmd.CommandText = "SELECT Id, StudentNumber, NoteText, CreatedAt FROM StudentNotes WHERE StudentNumber = @sn ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("@sn", studentNumber);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new StudentNote
                {
                    Id = reader.GetInt32(0),
                    StudentNumber = reader.GetString(1),
                    NoteText = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3))
                });
            }

            return list;
        }

        public StudentNote AddNote(string studentNumber, string noteText)
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            
            try
            {
                using var cmd = conn.CreateCommand();
                var createdAt = DateTime.Now;
                
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO StudentNotes (StudentNumber, NoteText, CreatedAt)
                    VALUES (@sn, @text, @date);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@sn", studentNumber);
                cmd.Parameters.AddWithValue("@text", noteText);
                cmd.Parameters.AddWithValue("@date", createdAt.ToString("o"));

                var id = Convert.ToInt32(cmd.ExecuteScalar());

                // Cache latest note to Students table
                using var updateCmd = conn.CreateCommand();
                updateCmd.Transaction = tx;
                updateCmd.CommandText = "UPDATE Students SET SpecialNote = @text WHERE StudentNumber = @sn;";
                updateCmd.Parameters.AddWithValue("@sn", studentNumber);
                updateCmd.Parameters.AddWithValue("@text", noteText);
                updateCmd.ExecuteNonQuery();

                tx.Commit();

                return new StudentNote
                {
                    Id = id,
                    StudentNumber = studentNumber,
                    NoteText = noteText,
                    CreatedAt = createdAt
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void DeleteNote(int noteId, string studentNumber)
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StudentNotes WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", noteId);
                cmd.ExecuteNonQuery();

                // Update cache with the most recent note, if any, or empty
                using var updateCmd = conn.CreateCommand();
                updateCmd.Transaction = tx;
                updateCmd.CommandText = @"
                    UPDATE Students 
                    SET SpecialNote = COALESCE(
                        (SELECT NoteText FROM StudentNotes WHERE StudentNumber = @sn ORDER BY Id DESC LIMIT 1),
                        ''
                    )
                    WHERE StudentNumber = @sn;
                ";
                updateCmd.Parameters.AddWithValue("@sn", studentNumber);
                updateCmd.ExecuteNonQuery();

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Öğrencinin hiç notu olup olmadığını hızlıca kontrol eder (UI'daki icon tasarımı için).
        /// </summary>
        public bool HasAnyNotes(string studentNumber)
        {
            if (string.IsNullOrWhiteSpace(studentNumber)) return false;

            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM StudentNotes WHERE StudentNumber = @sn LIMIT 1;";
            cmd.Parameters.AddWithValue("@sn", studentNumber);
            
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }
    }
}
