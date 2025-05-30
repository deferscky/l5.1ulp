using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

public interface IProtocol
{
    string Protocol();
}

[JsonPolymorphic]
[JsonDerivedType(typeof(Email), nameof(Email))]
[JsonDerivedType(typeof(SMS), nameof(SMS))]
[JsonDerivedType(typeof(Letter), nameof(Letter))]
[JsonDerivedType(typeof(ElectronicMessage), nameof(ElectronicMessage))]
public abstract class Message : IProtocol
{
    public abstract string Protocol();
    public abstract void PrintDetails();
}

public class MessageStorage
{
    public List<Message> Messages { get; set; } = new List<Message>();
}

public class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { AddTypeDiscriminator }
        }
    };

    private static void AddTypeDiscriminator(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(Message)) return;

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            IgnoreUnrecognizedTypeDiscriminators = true,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
            DerivedTypes =
            {
                new JsonDerivedType(typeof(Email), "Email"),
                new JsonDerivedType(typeof(SMS), "SMS"),
                new JsonDerivedType(typeof(Letter), "Letter"),
                new JsonDerivedType(typeof(ElectronicMessage), "ElectronicMessage")
            }
        };
    }

    static void Main()
    {
        string filePath = "messages.json";
        try
        {
            InitializeFile(filePath);

            AddMessage(filePath, new Email("bobr@mail.ru", "crocodil@mail.ru", "Hello!"));
            AddMessage(filePath, new SMS("+7900-800-10-10", "Hi!"));
            AddMessage(filePath, new Letter("Staropupunski 12", "Pushkina 120", "This is a letter."));
            AddMessage(filePath, new ElectronicMessage("This is an electronic message."));

            FindMessage(filePath, "Hello!");
            RemoveMessage(filePath, "Hi!");
            FindMessage(filePath, "Hi!");

            Console.WriteLine($"Файл сохранен по пути: {Path.GetFullPath(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }

    static void InitializeFile(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации файла: {ex.Message}");
            throw;
        }
    }

    static void AddMessage(string filePath, Message message)
    {
        try
        {
            var storage = LoadMessages(filePath);
            storage.Messages.Add(message);
            SaveMessages(filePath, storage);
            Console.WriteLine($"Сообщение типа {message.GetType().Name} добавлено");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении сообщения: {ex.Message}");
        }
    }

артем, [21.04.2025 17:26]
static void RemoveMessage(string filePath, string content)
    {
        try
        {
            var storage = LoadMessages(filePath);
            int removed = storage.Messages.RemoveAll(m => 
                m is ElectronicMessage em && em.Content == content ||
                m is SMS sms && sms.Text == content);if (removed > 0)
            {
                SaveMessages(filePath, storage);
                Console.WriteLine($"Сообщение с содержанием '{content}' удалено");
            }
            else
            {
                Console.WriteLine($"Сообщение не найдено");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
        }
    }

    static void FindMessage(string filePath, string searchText)
    {
        try
        {
            var storage = LoadMessages(filePath);
            var found = storage.Messages.Find(m =>
                m switch
                {
                    Email e => e.Subject.Contains(searchText),
                    SMS s => s.Text.Contains(searchText),
                    Letter l => l.Content.Contains(searchText),
                    ElectronicMessage em => em.Content.Contains(searchText),
                    _ => false
                });

            if (found != null)
            {
                Console.WriteLine("Найдено сообщение:");
                found.PrintDetails();
                Console.WriteLine($"Протокол: {found.Protocol()}\n");
            }
            else
            {
                Console.WriteLine($"Сообщение с текстом '{searchText}' не найдено\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске сообщения: {ex.Message}");
        }
    }

    static MessageStorage LoadMessages(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return new MessageStorage();
            
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<MessageStorage>(json, JsonOptions) ?? new MessageStorage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            return new MessageStorage();
        }
    }

    static void SaveMessages(string filePath, MessageStorage storage)
    {
        try
        {
            string json = JsonSerializer.Serialize(storage, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения данных: {ex.Message}");
        }
    }
}

public class Email : Message
{
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public string Subject { get; set; }

    public Email() { }

    public Email(string sender, string recipient, string subject)
    {
        Sender = sender;
        Recipient = recipient;
        Subject = subject;
    }

    public override string Protocol() => "Email Protocol";
    
    public override void PrintDetails()
    {
        Console.WriteLine($"От: {Sender}\nКому: {Recipient}\nТема: {Subject}");
    }
}

public class SMS : Message
{
    public string PhoneNumber { get; set; }
    public string Text { get; set; }

    public SMS() { }

    public SMS(string phoneNumber, string text)
    {
        PhoneNumber = phoneNumber;
        Text = text;
    }

    public override string Protocol() => "SMS Protocol";
    
    public override void PrintDetails()
    {
        Console.WriteLine($"Номер: {PhoneNumber}\nТекст: {Text}");
    }
}

public class Letter : Message
{
    public string SenderAddress { get; set; }
    public string RecipientAddress { get; set; }
    public string Content { get; set; }

    public Letter() { }

    public Letter(string senderAddress, string recipientAddress, string content)
    {
        SenderAddress = senderAddress;
        RecipientAddress = recipientAddress;
        Content = content;
    }

артем, [21.04.2025 17:26]
public override string Protocol() => "Letter Protocol";
    
    public override void PrintDetails()
    {
        Console.WriteLine($"Адрес отправителя: {SenderAddress}\nАдрес получателя: {RecipientAddress}\nСодержание: {Content}");
    }
}

public class ElectronicMessage : Message
{
    public string Content { get; set; }

    public ElectronicMessage() { }
public ElectronicMessage(string content)
    {
        Content = content;
    }

    public override string Protocol() => "Electronic Message Protocol";
    
    public override void PrintDetails()
    {
        Console.WriteLine($"Содержание: {Content}");
    }
}
