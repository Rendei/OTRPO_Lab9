// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const ws = new WebSocket(`ws://${window.location.hostname}:8081/ws`);

const messagesDiv = document.getElementById("messages");
const usersList = document.getElementById("users-list");
const input = document.getElementById("message-input");
const sendButton = document.getElementById("send-button");

ws.onmessage = (event) => {
    const data = event.data;

    if (data.startsWith("ONLINE_USERS:")) {
        // Обновление списка пользователей
        const users = data.replace("ONLINE_USERS:", "").split(",");
        updateUsersList(users);
    } else {
        // Добавление нового сообщения
        addMessage(data);
    }
};

// Обновление списка участников
function updateUsersList(users) {
    // Очищаем текущий список
    usersList.innerHTML = "";

    // Добавляем каждого пользователя
    users.forEach((user) => {
        const userItem = document.createElement("li");
        userItem.textContent = user;
        usersList.appendChild(userItem);
    });
}

// Добавление нового сообщения
function addMessage(message) {
    const messageDiv = document.createElement("div");
    messageDiv.textContent = message;
    messagesDiv.appendChild(messageDiv);

    // Прокручиваем сообщения вниз, чтобы последнее было видно
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

sendButton.addEventListener("click", () => {
    const text = input.value.trim();
    if (text) {
        ws.send(text);
        input.value = "";
    }
});

input.addEventListener("keypress", (e) => {
    if (e.key === "Enter") {
        sendButton.click();
    }
});
