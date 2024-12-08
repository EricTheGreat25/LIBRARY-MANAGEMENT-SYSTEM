using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibraryManageSystem
{
    interface ISearchable
    {
        void SearchBook(List<Book> books);
    }

    interface IBorrowable
    {
        void BorrowBook(List<Book> books);
        void ReturnBook(List<Book> books);
    }
    interface IViewAccount
    {
        void ViewAccount();
    }

    interface IManageable
    {
        void ViewAllUsers(Dictionary<string, Account> users);
    }

    public abstract class Person
    {
        public string Name { get; private set; }
        public string Id { get; private set; }

        protected Person(string name, string id)
        {
            this.Name = name;
            this.Id = id;
        }
    }

    public class User : Person, ISearchable
    {
        public User(string name, string id) : base(name, id) { }

        public void SearchBook(List<Book> books)
        {
            Console.Clear();

            if (books.Count == 0)
            {
                Console.WriteLine("No books are available in the library.");
                Console.WriteLine("Press any key to return...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("BOOK SEARCH BY GENRE");

            var genres = books.Select(b => b.Genre).Distinct().ToList();

            for (int i = 0; i < genres.Count; i++)
            {
                Console.WriteLine($"({i + 1}) {genres[i]}");
            }
            Console.Write("\nSelect a genre by number: ");
            if (!int.TryParse(Console.ReadLine(), out int genreChoice) || genreChoice < 1 || genreChoice > genres.Count)
            {
                Console.WriteLine("Invalid selection. Returning to menu.");
                return;
            }

            string selectedGenre = genres[genreChoice - 1];
            var matchingBooks = books.Where(b => b.Genre.Equals(selectedGenre, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matchingBooks.Any())
            {
                Console.WriteLine($"\nBooks in the genre: {selectedGenre}");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"{"Title",-30} {"Author",-20} {"ISBN",-15} {"Due Date",-10}");
                Console.WriteLine(new string('-', 80));

                foreach (var book in matchingBooks)
                {
                    Console.WriteLine($"{book.Title,-30} {book.Author,-20} {book.ISBN,-15} {book.DueDate,-10}");
                }

                Console.WriteLine(new string('-', 80));
            }
            else
            {
                Console.WriteLine($"No books found in the genre: {selectedGenre}");
            }

            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }

        public string ToFileFormat()
        {
            return $"{Id},{Name}";
        }
        public static Account FromFileFormat(string line)
        {
            var parts = line.Split(',');
            if (parts.Length == 2)
            {
                return new Account(parts[1], parts[0]); 
            }
            Console.WriteLine("Invalid user data format.");
            return null;
        }

    }


    public class Account : User, IBorrowable, IViewAccount
    {
        public float FineAmount { get; private set; }
        public int NoOfBorrowedBooks { get; private set; }
        private List<Book> borrowedBooks;

        public Account(string name, string id) : base(name, id)
        {
            FineAmount = 0.0f;
            NoOfBorrowedBooks = 0;
            borrowedBooks = new List<Book>();
        }
        public void BorrowBook(List<Book> books)
        {
            while (true)
            {
                Console.Clear();

                if (books.Count == 0)
                {
                    Console.WriteLine("No books are available in the library.");
                    Console.WriteLine("Press any key to return...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("AVAILABLE BOOKS:");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"{"Index",-5} {"Title",-30} {"Author",-20} {"ISBN",-15} {"Genre",-10}");
                Console.WriteLine(new string('-', 80));

                for (int i = 0; i < books.Count; i++)
                {
                    var book = books[i];
                    Console.WriteLine($"{i + 1,-5} {book.Title,-30} {book.Author,-20} {book.ISBN,-15} {book.Genre,-10}");
                }

                Console.WriteLine(new string('-', 80));
                Console.WriteLine("\n(0) Back to Options");
                Console.Write("\nEnter the index of the book you want to borrow (or 0 to go back): ");

                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > books.Count)
                {
                    Console.WriteLine("Invalid input. Please enter a valid number.");
                    Console.ReadKey();
                    continue;
                }

                if (choice == 0)
                {
                    Console.WriteLine("Returning to options...");
                    Console.ReadKey();
                    return;
                }

                Book selectedBook = books[choice - 1];

                if (selectedBook.IsReserved)
                {
                    Console.WriteLine($"Sorry, the book '{selectedBook.Title}' is already reserved.");
                }
                else
                {
                    borrowedBooks.Add(selectedBook);
                    books.Remove(selectedBook);
                    NoOfBorrowedBooks++;
                    selectedBook.IsReserved = true; 
                    Console.WriteLine($"You have successfully borrowed '{selectedBook.Title}'. Enjoy reading!");
                }

                Console.WriteLine("\nPress any key to return to the borrowing menu...");
                Console.ReadKey();
            }
        }
        public void ReturnBook(List<Book> books)
        {
            if (borrowedBooks.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No books to return.");
                return;
            }

            Console.Clear();
            Console.WriteLine("Borrowed Books:");
            Console.WriteLine(new string('-', 80)); 
            Console.WriteLine("| {0,-5} | {1,-30} | {2,-20} | {3,-15} | {4,-15} |", "No.", "Title", "Author", "Due Date", "Status");
            Console.WriteLine(new string('-', 80)); 
            for (int i = 0; i < borrowedBooks.Count; i++)
            {
                Book book = borrowedBooks[i];
                Console.WriteLine("| {0,-5} | {1,-30} | {2,-20} | {3,-15} | {4,-15} |",
                    i + 1,             
                    book.Title,          
                    book.Author,          
                    book.DueDate,        
                    "Borrowed");            
            }

            Console.WriteLine(new string('-', 80));
            Console.Write("Select a book number to return: ");

            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= borrowedBooks.Count)
            {
                Book selectedBook = borrowedBooks[choice - 1]; 
                selectedBook.IsReserved = false; 
                borrowedBooks.Remove(selectedBook); 
                books.Add(selectedBook); 
                NoOfBorrowedBooks--;
                Console.WriteLine($"{selectedBook.Title} returned.");
                FileHandler.UpdateUsers(FileHandler.GetUsers());
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }
        private void CalculateFine()
        {
            FineAmount = 0.0f;

            foreach (var book in borrowedBooks)
            {
                DateTime dueDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, book.DueDate);

                if (DateTime.Now > dueDate)
                {
                    TimeSpan overdueDays = DateTime.Now - dueDate;
                    FineAmount += (float)(overdueDays.Days * 0.50); 
                }
            }

        }
        public void ViewAccount()
        {
            CalculateFine(); 
            Console.Clear();
            Console.WriteLine("USER ACCOUNT DETAILS");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("| {0,-25} | {1,-20} |", "Field", "Value");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("| {0,-25} | {1,-20} |", "Name", Name);
            Console.WriteLine("| {0,-25} | {1,-20} |", "User ID", Id);
            Console.WriteLine("| {0,-25} | {1,-20} |", "Number of Borrowed Books", NoOfBorrowedBooks);
            Console.WriteLine("| {0,-25} | {1,-20} |", "Total Fine", FineAmount > 0 ? $"{FineAmount:F2}" : "No fine");

            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
        public string BorrowedBooksToString()
        {
            return string.Join(",", borrowedBooks.ConvertAll(book => book.ToString()));
        }

        public void LoadBorrowedBooks(string borrowedBooksData)
        {
            if (!string.IsNullOrEmpty(borrowedBooksData))
            {
                foreach (var bookData in borrowedBooksData.Split(','))
                {
                    borrowedBooks.Add(Book.FromString(bookData));
                }
            }
        }
    }

    public class Book
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public string ISBN { get; private set; }
        public int DueDate { get; private set; }
        public bool IsReserved { get; set; }
        public string Genre { get; private set; }

        public Book(string title, string author, string isbn, int dueDate, bool isReserved, string genre)
        {
            this.Title = title;
            this.Author = author;
            this.ISBN = isbn;
            this.DueDate = dueDate;
            this.IsReserved = isReserved;
            this.Genre = genre;
        }

        public override string ToString()
        {
            return $"{Title}|{Author}|{ISBN}|{DueDate}|{IsReserved}|{Genre}";
        }

        public string ToFileFormat()
        {
            return $"{Title}|{Author}|{ISBN}|{DueDate}|{IsReserved}|{Genre}";
        }
        public static Book FromString(string line)
        {
            var parts = line.Split('|');
            if (parts.Length == 6)  
            {
                return new Book(
                    parts[0],          
                    parts[1],           
                    parts[2],           
                    int.Parse(parts[3]), 
                    bool.Parse(parts[4]), 
                    parts[5]            
                );
            }
            return null; 
        }

    }

    public class Librarian : Person, IManageable
    {
        private string password;

        public Librarian(string name, string id, string password) : base(name, id)
        {
            this.password = password;
        }
        public bool VerifyLibrarian(string id, string password)
        {
            return Id == id && password == password;
        }
        public void ViewAllUsers(Dictionary<string, Account> users)
        {
            Console.Clear();
            Console.WriteLine("Registered Users:");

            Console.Write("Search by Name or ID (press Enter to skip): ");
            string searchTerm = Console.ReadLine()?.Trim().ToLower();

            var filteredUsers = users.Values.Where(u =>
                string.IsNullOrEmpty(searchTerm) ||
                u.Name.ToLower().Contains(searchTerm) || u.Id.Contains(searchTerm)
            ).ToList();

            if (filteredUsers.Count == 0)
            {
                Console.WriteLine("No users found matching your search.");
            }
            else
            {
                Console.WriteLine("| {0,-20} | {1,-10} | {2,10} | {3,20} |", "Name", "ID", "Fine Amount", "Books Borrowed");
                Console.WriteLine(new string('-', 70));  

                foreach (var user in filteredUsers)
                {
                    string fineAmount = user.FineAmount > 0 ? $"{user.FineAmount:F2}" : "N/A";
                    Console.WriteLine("| {0,-20} | {1,-10} | {2,10} | {3,20} |",
                        user.Name,
                        user.Id,
                        fineAmount,
                        user.NoOfBorrowedBooks); 
                }
            }

            Console.WriteLine(new string('-', 70));
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }

        public void DeleteBook(List<Book> books)
        {
            if (books.Count == 0)
            {
                Console.WriteLine("No books available to delete.");
                return;
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Index",-5} {"Title",-30} {"Author",-20} {"Genre",-15}");
            Console.WriteLine(new string('-', 80));

            for (int i = 0; i < books.Count; i++)
            {
                Console.WriteLine($"{i + 1,-5} {books[i].Title,-30} {books[i].Author,-20} {books[i].Genre,-15}");
            }

            Console.WriteLine(new string('-', 80));

            Console.Write("Enter the index number of the book you want to delete: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= books.Count)
            {
                var bookToDelete = books[index - 1];
                Console.Write($"Are you sure you want to delete '{bookToDelete.Title}' by {bookToDelete.Author}? (y/n): ");
                string confirmation = Console.ReadLine()?.ToLower();

                if (confirmation == "y")
                {
                    books.RemoveAt(index - 1); 
                    Console.WriteLine($"'{bookToDelete.Title}' has been successfully deleted.");
                }
                else
                {
                    Console.WriteLine("Deletion canceled.");
                }
            }
            else
            {
                Console.WriteLine("Invalid selection. Returning to menu.");
            }
        }
        public void ViewBooks(List<Book> books)
        {
            Console.Clear();
            Console.WriteLine("View Books Options:");
            Console.WriteLine("1. View All Books");
            Console.WriteLine("2. Search Books by Title or Genre");
            Console.Write("Please choose an option (1 or 2): ");

            if (int.TryParse(Console.ReadLine(), out int choice))
            {
                switch (choice)
                {
                    case 1:
                        DisplayBooks(books);
                        break;

                    case 2:
                        SearchBooks(books);
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please choose 1 or 2.");
                        Console.ReadKey();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid option.");
                Console.ReadKey();
            }
        }

        private void DisplayBooks(List<Book> books)
        {
            Console.Clear();
            if (books.Count == 0)
            {
                Console.WriteLine("No books are currently available in the library.");
                Console.WriteLine("Press any key to return...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("BOOKS IN LIBRARY:");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Title",-30} {"Author",-20} {"ISBN",-15} {"Genre",-10} {"Reserved",-10}");
            Console.WriteLine(new string('-', 80));

            foreach (var book in books)
            {
                Console.WriteLine($"{book.Title,-30} {book.Author,-20} {book.ISBN,-15} {book.Genre,-10} {book.IsReserved,-10}");
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }
        private void SearchBooks(List<Book> books)
        {
            Console.Clear();
            Console.Write("Enter a search term (Title or Genre): ");
            string searchTerm = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("Search term cannot be empty. Returning to menu...");
                Console.ReadKey();
                return;
            }

            var filteredBooks = books.Where(b =>
                b.Title.ToLower().Contains(searchTerm) ||  
                b.Genre.ToLower() == searchTerm  
            ).ToList();

            if (filteredBooks.Count == 0)
            {
                Console.WriteLine($"No books found matching the term '{searchTerm}'.");
                Console.WriteLine("Please search again.");
            }
            else
            {
                Console.WriteLine($"Books matching '{searchTerm}':");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"{"Title",-30} {"Author",-20} {"ISBN",-15} {"Genre",-10} {"Reserved",-10}");
                Console.WriteLine(new string('-', 80));

                foreach (var book in filteredBooks)
                {
                    Console.WriteLine($"{book.Title,-30} {book.Author,-20} {book.ISBN,-15} {book.Genre,-10} {book.IsReserved,-10}");
                }
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }
        public void DeleteUser(Dictionary<string, Account> users)
        {
            Console.WriteLine("Registered Users:");
            foreach (var user in users.Values)
            {
                Console.WriteLine($"User ID: {user.Id}, Name: {user.Name}");
            }

            Console.Write("Enter the User ID of the user you want to delete: ");
            string userIdToDelete = Console.ReadLine();

            if (users.ContainsKey(userIdToDelete))
            {
                Console.Write($"Are you sure you want to delete the user with ID {userIdToDelete}? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    users.Remove(userIdToDelete);
                    FileHandler.DeleteUser(userIdToDelete);
                    FileHandler.UpdateUsers(users);
                    Console.WriteLine("User deleted successfully.");
                }
                else
                {
                    Console.WriteLine("User deletion canceled.");
                }
            }
            else
            {
                Console.WriteLine("User ID not found.");
            }
        }
        public void AddBook(List<Book> books)
        {
            while (true)
            {
                Console.Clear();
                Console.Write("Enter Book Title: ");
                string title = Console.ReadLine();
                Console.Write("Enter Book Author: ");
                string author = Console.ReadLine();
                Console.Write("Enter Book ISBN: ");
                string isbn = Console.ReadLine();
                Console.Write("Enter Due Date (as day of the month): ");

                if (!int.TryParse(Console.ReadLine(), out int dueDate))
                {
                    Console.WriteLine("Invalid due date format.");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey();
                    continue; 
                }

                Console.WriteLine("Select Genre:");
                string[] genres = {
            "Fiction", "Non-fiction", "Reference books", "Textbooks",
            "Biographies", "Autobiographies and memoirs", "Religious books",
            "Scientific books", "Historical books", "Cookbooks, crafts, and hobbies",
            "Business and economics"
        };

                for (int i = 0; i < genres.Length; i++)
                {
                    Console.WriteLine($"({i + 1}) {genres[i]}");
                }

                Console.Write("Enter the number corresponding to the genre: ");
                if (!int.TryParse(Console.ReadLine(), out int genreIndex) || genreIndex < 1 || genreIndex > genres.Length)
                {
                    Console.WriteLine("Invalid genre selection.");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey();
                    continue;
                }

                string genre = genres[genreIndex - 1];

                if (books.Any(b => b.ISBN == isbn))
                {
                    Console.WriteLine("This book already exists in the library.");
                }
                else
                {
                    Book book = new Book(title, author, isbn, dueDate, false, genre);
                    books.Add(book);
                    FileHandler.UpdateBooks(books); 
                    Console.WriteLine($"Book '{title}' added successfully.");
                }

                Console.Write("Do you want to add another book? (y/n): ");
                string choice = Console.ReadLine()?.ToLower();

                if (choice != "y")
                {
                    Console.WriteLine("Returning to menu...");
                    break; 
                }
            }
        }
        public string ToFileFormat()
        {
            return $"{Name}|{Id}|{password}";
        }

        public static Librarian FromString(string line)
        {
            var parts = line.Split('|');
            if (parts.Length == 3)  
            {
                return new Librarian(parts[0], parts[1], parts[2]);
            }
            return null;  
        }
}

    class FileHandler
    {
        private const string BookFilePath = "books.txt";
        private const string UserFilePath = "users.txt";
        private const string LibrarianFilePath = "librarians.txt";

        public static void AddBook(Book book)
        {
            try
            {
                var books = GetBooks(); 
                if (books.Any(b => b.ISBN == book.ISBN)) 
                {
                    Console.WriteLine("This book already exists in the library.");
                    return;
                }
                books.Add(book);
                UpdateBooks(books);
                Console.WriteLine("Book added successfully.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error adding book: {ex.Message}");
            }
        }

        public static List<Book> GetBooks()
        {
            var books = new List<Book>();
            try
            {
                if (File.Exists(BookFilePath))
                {
                    var lines = File.ReadAllLines(BookFilePath);
                    foreach (var line in lines)
                    {
                        var book = Book.FromString(line);
                        if (book != null)
                        {
                            books.Add(book);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading books file: {ex.Message}");
            }
            return books;
        }

        public static void UpdateBooks(List<Book> books)
        {
            try
            {
                using (var writer = new StreamWriter(BookFilePath, false)) 
                {
                    foreach (var book in books)
                    {
                        writer.WriteLine(book.ToFileFormat());
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error updating books file: {ex.Message}");
            }
        }
        public static void AddUser(Account user)
        {
            try
            {
                using (var writer = new StreamWriter(UserFilePath, append: true))
                {
                    writer.WriteLine(user.ToFileFormat());
                }
                Console.WriteLine("User added successfully.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
            }
        }

        public static Dictionary<string, Account> GetUsers()
        {
            var users = new Dictionary<string, Account>();
            try
            {
                if (File.Exists(UserFilePath))
                {
                    var lines = File.ReadAllLines(UserFilePath);
                    foreach (var line in lines)
                    {
                        var user = Account.FromFileFormat(line);
                        if (user != null)
                        {
                            users[user.Id] = user; 
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
            }
            return users;
        }
        public static void UpdateUsers(Dictionary<string, Account> users)
        {
            try
            {
                using (var writer = new StreamWriter(UserFilePath, false))
                {
                    foreach (var user in users.Values)
                    {
                        writer.WriteLine(user.ToFileFormat());
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error updating users file: {ex.Message}");
            }
        }
        public static void DeleteUser(string userId)
        {
            try
            {
                var users = GetUsers();
                if (users.Remove(userId))
                {
                    UpdateUsers(users); 
                    Console.WriteLine($"User with ID {userId} deleted successfully.");
                }
                else
                {
                    Console.WriteLine("User not found.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
            }
        }
        public static void SaveLibrarian(Librarian librarian)
        {
            try
            {
                using (var writer = new StreamWriter(LibrarianFilePath, append: false)) 
                {
                    writer.WriteLine(librarian.ToFileFormat()); 
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error saving librarian: {ex.Message}");
            }
        }

        public static Librarian GetLibrarian()
        {
            if (File.Exists(LibrarianFilePath))
            {
                var line = File.ReadAllText(LibrarianFilePath);
                return Librarian.FromString(line);
            }
            return null;
        }

        public static void UpdateLibrarian(Librarian librarian)
        {
            SaveLibrarian(librarian);
        }
    }
    public class Menu
    {
        public static void HandleUser(Dictionary<string, Account> users, List<Book> books)
        {
            bool backToMainMenu = false;

            while (!backToMainMenu)
            {
                Console.Clear();
                Console.WriteLine("DO YOU WANT TO LOG IN OR SIGN UP?");
                Console.WriteLine("(1) SIGN UP");
                Console.WriteLine("(2) LOG IN");
                Console.WriteLine("(0) Back to Main Menu");
                Console.Write("Enter your choice (0/1/2): ");

                if (int.TryParse(Console.ReadLine(), out int userTypeChoice))
                {
                    switch (userTypeChoice)
                    {
                        case 0: 
                            Console.Clear();
                            backToMainMenu = true;
                            Console.WriteLine("Returning to main menu...");
                            break;

                        case 1: 
                            Console.Clear();
                            Console.Write("Enter your name: ");
                            string name = Console.ReadLine();
                            Console.Write("Enter your ID: ");
                            string userId = Console.ReadLine();

                            if (string.IsNullOrEmpty(userId))
                            {
                                Console.WriteLine("User ID cannot be empty. Returning to menu...");
                                Console.ReadKey();
                                break;
                            }

                            if (users.ContainsKey(userId))
                            {
                                Console.WriteLine("User ID already exists. Please choose a different ID.");
                                Console.ReadKey();
                                break;
                            }

                            Account newUser = new Account(name, userId);
                            users.Add(userId, newUser);
                            FileHandler.AddUser(newUser);
                            Console.WriteLine("User registered successfully.");
                            currentUserActions(newUser, books);
                            break;

                        case 2: 
                            Console.Write("Enter your ID: ");
                            string existingUserId = Console.ReadLine();

                            if (users.TryGetValue(existingUserId, out Account existingUser))
                            {
                                Console.WriteLine($"Welcome back, {existingUser.Name}!");
                                currentUserActions(existingUser, books);
                            }
                            else
                            {
                                Console.WriteLine("User not found. Please check your ID or register as a new user.");
                            }
                            Console.ReadKey();
                            break;

                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    Console.ReadKey();
                }
            }
        }

        public static void HandleLibrarian(ref Librarian librarian, List<Book> books, Dictionary<string, Account> users)
        {
            bool backToMainMenu = false;

            while (!backToMainMenu)
            {
                Console.Clear();
                Console.WriteLine("Are you a new or an existing librarian?");
                Console.WriteLine("(1) New Librarian");
                Console.WriteLine("(2) Existing Librarian");
                Console.WriteLine("(0) Back to Main Menu");
                Console.Write("Enter your choice (0/1/2): ");

                if (int.TryParse(Console.ReadLine(), out int librarianTypeChoice))
                {
                    switch (librarianTypeChoice)
                    {
                        case 0: 
                            Console.Clear();
                            backToMainMenu = true;
                            Console.WriteLine("Returning to main menu...");
                            break;

                        case 1: 
                            Console.Clear();
                            Console.Write("Enter your name: ");
                            string name = Console.ReadLine();
                            Console.Write("Enter a librarian ID: ");
                            string id = Console.ReadLine();
                            Console.Write("Enter a password: ");
                            string password = Console.ReadLine();

                            librarian = new Librarian(name, id, password);
                            FileHandler.SaveLibrarian(librarian);
                            Console.WriteLine("Librarian registered successfully.\n");
                            LibrarianActionsMenu(librarian, books, users);
                            break;

                        case 2:
                            if (librarian == null)
                            {
                                Console.WriteLine("No registered librarian found. Please register as a new librarian.");
                                Console.ReadKey();
                                break; 
                            }
                            Console.Clear();
                            Console.Write("Enter your librarian ID: ");
                            string existingId = Console.ReadLine();
                            Console.Write("Enter your password: ");
                            string existingPassword = Console.ReadLine();

                            if (librarian.VerifyLibrarian(existingId, existingPassword))
                            {
                                Console.WriteLine("Authentication successful.\n");
                                LibrarianActionsMenu(librarian, books, users); 
                            }
                            else
                            {
                                Console.WriteLine("Invalid ID or password. Please try again.");
                                Console.ReadKey();
                            }
                            break;

                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    Console.ReadKey();
                }
            }
        }
        static void currentUserActions(Account currentUser, List<Book> books)
        {
            bool continueActions = true;

            while (continueActions)
            {
                Console.Clear();
                Console.WriteLine(new string('*', 50));
                Console.WriteLine($"* WELCOME USER: {currentUser.Name.ToUpper()}".PadRight(49) + "*");
                Console.WriteLine(new string('*', 50));
                Console.WriteLine("\nSelect an option:");
                Console.WriteLine("(1) Search for a book");
                Console.WriteLine("(2) Borrow a book");
                Console.WriteLine("(3) Return a book");
                Console.WriteLine("(4) View Account");
                Console.WriteLine("(0) Exit");

                Console.Write("Enter your choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        currentUser.SearchBook(books);
                        break;

                    case "2":
                        currentUser.BorrowBook(books);
                        break;

                    case "3":
                        Console.Clear();
                        currentUser.ReturnBook(books);
                        break;
                    case "4":
                        currentUser.ViewAccount();
                        break;

                    case "0":
                        continueActions = false;
                        Console.WriteLine("Returning to main menu...");
                        break;

                    default:
                        Console.WriteLine("Invalid choice, please enter a valid option.");
                        break;
                }
                if (continueActions)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }
        static void LibrarianActionsMenu(Librarian librarian, List<Book> books, Dictionary<string, Account> users)
        {
            bool stayInLibrarianMenu = true;
            while (stayInLibrarianMenu)
            {
                Console.Clear();
                Console.WriteLine(new string('*', 50));
                Console.WriteLine($"* WELCOME LIBRARIAN: {librarian.Name.ToUpper()}".PadRight(49) + "*");
                Console.WriteLine(new string('*', 50));
                Console.WriteLine("\nLIBRARIAN");
                Console.WriteLine("(1) Add Book");
                Console.WriteLine("(2) Delete Book");
                Console.WriteLine("(3) View Books");
                Console.WriteLine("(4) View All Users");
                Console.WriteLine("(5) Delete User");
                Console.WriteLine("(0) Back to Main Menu");
                Console.Write("Enter your choice: ");

                string librarianChoice = Console.ReadLine();

                switch (librarianChoice)
                {
                    case "1":
                        librarian.AddBook(books);
                        break;
                    case "2":
                        Console.Clear();
                        librarian.DeleteBook(books);
                        FileHandler.UpdateBooks(books);
                        break;
                    case "3":
                        librarian.ViewBooks(books);
                        break;

                    case "4":
                        librarian.ViewAllUsers(users);
                        break;

                    case "5":
                        Console.Clear();
                        librarian.DeleteUser(users);
                        break;

                    case "0":
                        stayInLibrarianMenu = false;
                        Console.WriteLine("Returning to main menu...");
                        break;

                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }

                if (stayInLibrarianMenu)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            List<Book> books = FileHandler.GetBooks();
            Dictionary<string, Account> users = FileHandler.GetUsers();
            Librarian librarian = FileHandler.GetLibrarian();

            while (true)
            {
                Console.Clear();
                Console.WriteLine(new string('=', 70));
                Console.WriteLine("=".PadRight(69) + "=");
                Console.WriteLine($"={"WELCOME TO LIBRARY MANAGEMENT SYSTEM".PadLeft(44).PadRight(68)}=");
                Console.WriteLine("=".PadRight(69) + "=");
                Console.WriteLine(new string('=', 70));
                Console.WriteLine("MAIN MENU");
                Console.WriteLine("(1) USER");
                Console.WriteLine("(2) LIBRARIAN");
                Console.Write("Enter your choice (1/2) or 0 to Exit: ");
                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 0:
                            Console.Clear();
                            int width = Console.WindowWidth;
                            string message = "Exiting the system. \nTHANK YOU! \nGoodbye!";
                            int spaces = (width - message.Length) / 2;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(new string(' ', spaces) + message);
                            Console.ResetColor();
                            Console.ReadKey();

                            FileHandler.UpdateUsers(users); 
                            FileHandler.UpdateBooks(books); 
                            FileHandler.UpdateLibrarian(librarian); 
                            return; 

                        case 1:
                            Menu.HandleUser(users, books);
                            break;

                        case 2:
                            Menu.HandleLibrarian(ref librarian, books, users);
                            break;

                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    Console.ReadKey();
                }
            }
        }
    }
}