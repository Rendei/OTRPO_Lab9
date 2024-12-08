// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let username = null; 
const sendButton = document.getElementById("send-button");
const joinButton = document.getElementById("join-button");
const input = document.getElementById("message-input");
let currentRoomId = null;
let currentSocket = null;

joinButton.addEventListener("click", () => {
    const input = document.getElementById("username-input");
    username = input.value.trim();

    if (username) {
        // Скрываем экран логина и показываем чат
        document.getElementById("login-screen").style.display = "none";
        document.getElementById("chat-screen").style.display = "block";

        // Инициализируем подключение к чату
        connectToChat();
    } else {
        alert("Пожалуйста, введите имя пользователя!");
    }
});

input.addEventListener("keypress", (e) => {
    if (e.key === "Enter") {
        sendButton.click();
    }
});

function connectToChat() {
    if (currentSocket) {
        currentSocket.close(); // Закрываем предыдущее соединение
    }

    const socket = new WebSocket(`ws://${window.location.hostname}:8081/ws?username=${username}`);
    currentSocket = socket;

    socket.onopen = () => {
        console.log("Подключен к общему чату");
        currentRoomId = null; // Устанавливаем в null, чтобы обозначить подключение к общему чату
        document.getElementById("room-title").innerText = "Общий чат";
    };

    socket.onmessage = (event) => {
        const data = event.data;

        if (data.startsWith("ONLINE_USERS:")) {
            const users = data.replace("ONLINE_USERS:", "").split(",");
            updateOnlineUsers(users);
        } else if (!currentRoomId) {
            // Показываем сообщения только если мы в общем чате
            displayMessage(data);
        }
    };

    socket.onclose = () => {
        console.log("Соединение с общим чатом закрыто");
    };

    socket.onerror = (error) => {
        console.error("Ошибка в общем чате:", error);
    };

    sendButton.onclick = () => sendMessage(socket);

}


function updateOnlineUsers(users) {
    const usersList = document.getElementById("users-list");
    usersList.innerHTML = "";

    users.forEach((user) => {
        const li = document.createElement("li");
        li.textContent = user;
        usersList.appendChild(li);
    });
}

const sendMessage = (socket) => {
    const messageInput = document.getElementById("message-input");
    const message = messageInput.value.trim();

    if (message) {        
        socket.send(message); // Отправляем сообщение через WebSocket
        messageInput.value = ""; // Очищаем поле ввода
    }
};

function displayMessage(message) {
    const messagesDiv = document.getElementById("messages");

    if (currentRoomId) {
        const [roomPrefix, content] = message.split("|");
        if (!roomPrefix.includes(`room:${currentRoomId}`)) {
            return; // Игнорируем сообщения не из текущей комнаты
        }
        message = content; // Убираем префикс комнаты перед отображением
    }

    const messageElement = document.createElement("div");
    messageElement.textContent = message;
    messagesDiv.appendChild(messageElement);

    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

function clearMessages() {
    const messagesDiv = document.getElementById("messages");
    messagesDiv.innerHTML = ""; // Удаляем все сообщения из контейнера
}


document.getElementById("create-room-button").addEventListener("click", async () => {
    const privateRoomUser = document.getElementById("private-room-user").value.trim();

    if (privateRoomUser) {
        const response = await fetch(`/api/chat/create-room?user1=${username}&user2=${privateRoomUser}`, { method: "POST" });
        const data = await response.json();

        if (data.roomId) {
            connectToPrivateRoom(data.roomId);
        } else {
            alert("Не удалось создать комнату.");
        }
    } else {
        alert("Введите имя второго участника!");
    }
});

function connectToPrivateRoom(roomId) {
    if (currentSocket) {
        currentSocket.close(); // Закрываем предыдущее соединение
    }
    
    const socket = new WebSocket(`ws://${window.location.hostname}:8081/ws/private/${roomId}/?username=${username}`);
    currentSocket = socket;
    currentRoomId = roomId;

    socket.onopen = () => {
        console.log(`Подключен к приватной комнате: ${roomId}`);        
        document.getElementById("room-title").innerText = `Приватная комната: ${roomId}`;
        clearMessages();
        updateOnlineUsers([]);
        createLeaveRoomButton();
    };

    socket.onmessage = (event) => {
        const data = event.data;
        displayMessage(data);
    };

    socket.onclose = () => {
        console.log(`Отключен от комнаты: ${roomId}`);
        alert("Соединение с комнатой закрыто.");
        currentRoomId = null;
        connectToChat();
    };

    socket.onerror = (error) => {
        console.error("Ошибка в соединении с комнатой:", error);
    };

    sendButton.onclick = () => sendMessage(socket);

}

function joinPrivateRoom() {
    const roomId = document.getElementById("join-room-id").value.trim();

    if (roomId) {
        connectToPrivateRoom(roomId);

        document.getElementById("join-room-id").value = "";
    } else {
        alert("Введите ID комнаты!");
    }
}

function createLeaveRoomButton() {
    const chat = document.getElementById("chat-screen");

    const leaveRoomButton = document.createElement("button");
    leaveRoomButton.id = "leave-room-button";
    leaveRoomButton.textContent = "Выйти из комнаты";

    leaveRoomButton.addEventListener("click", () => {
        currentRoomId = null;
        clearMessages();
        connectToChat();
        document.getElementById("room-title").innerText = "Общий чат";
        leaveRoomButton.remove();
    });

    // Добавляем кнопку в элементы управления чатом
    chat.appendChild(leaveRoomButton);
}
