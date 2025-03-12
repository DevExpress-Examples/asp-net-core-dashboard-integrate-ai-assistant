DevExpress.localization.loadMessages({
    en: {
        'dxChat-emptyListMessage': 'Chat is Empty',
        'dxChat-emptyListPrompt': 'AI Assistant is ready to answer your questions.',
        'dxChat-textareaPlaceholder': 'Ask AI Assistant...',
    },
})

let AIChatCustomItem = (function() {

    const AI_CHAT_CUSTOM_ITEM = 'CHAT';

    const svgIcon = `<?xml version="1.0" encoding="utf-8"?>        
        <svg version="1.1" id="` + AI_CHAT_CUSTOM_ITEM + `" xmlns = "http://www.w3.org/2000/svg" xmlns:xlink = "http://www.w3.org/1999/xlink" x = "0px" y = "0px" viewBox = "0 0 32 32" style = "enable-background:new 0 0 24 24;" xml:space = "preserve" >
            <path class="dx-dashboard-accent-icon" d="M11.71 8.25C10.61 14.37 6.37 18.61.24 19.72c-.33.06-.33.51 0 .57 6.12 1.1 10.36 5.34 11.47
                11.47.06.33.51.33.57 0 1.1-6.12 5.34-10.36 11.47-11.47.33-.06.33-.51 0-.57-6.12-1.1-10.36-5.34-11.47-11.47a.288.288 0 00-.57
                0zM23.81.17c-.74 4.08-3.56 6.91-7.64 7.64-.22.04-.22.34 0 .38 4.08.74 6.91 3.56 7.64 7.64.04.22.34.22.38 0 .74-4.08 3.56-6.91
                7.64-7.64.22-.04.22-.34 0-.38-4.08-.74-6.91-3.56-7.64-7.64a.192.192 0 00-.38 0z" fill="#fff"></path>
        </svg>`;


    const aiChatMetadata = {
        bindings: [],
        icon: AI_CHAT_CUSTOM_ITEM,
        title: 'AI Assistant',
        index: 1
    };

    const assistant = {
        id: 'assistant',
        name: 'Virtual Assistant',
    };

    const user = {
        id: 'user',
    };

    class AIChat extends DevExpress.Dashboard.CustomItemViewer {
        constructor(model, $container, options, dashboardControl) {
            super(model, $container, options);

            this.chatId = '';
            this.lastUserQuery = '';
            this.lastRefreshButton = undefined;
            this.model.selectedSheet = undefined;
            this.model.updateChatItem = undefined;
            this.component = undefined;
            this.errorList = [];
            this.dashboardControl = dashboardControl;
            this.disabledSubscription = undefined;
            this.dashboardControl.on('dashboardInitialized', this.controlStateChangedHandler);
            this.dashboardControl.on('dashboardStateChanged', this.controlStateChangedHandler);
        }
        dispose() {
            super.dispose();
            this.disabledSubscription.dispose();
            this.dashboardControl.off('dashboardInitialized', this.controlStateChangedHandler);
            this.dashboardControl.off('dashboardStateChanged', this.controlStateChangedHandler);
        }
        
        async _tryFetch(fetchAction, message) {            
            try {
                return await fetchAction();
            }
            catch(error) {
                this._handleError({ message: error.message, code: message });
            }
        }

        _handleError(error) {
            const id = "id" + Math.random().toString(16).slice(2)
            setTimeout(() => {
                this.errorList = this.errorList.filter(err => err.id !== id);
                this.component.option('alerts', this.errorList);
            }, 10000);
            this.errorList.push({
                id: id,
                message: `${error.code} - ${error.message}`
            });
            this.component.option('alerts', this.errorList);
        }

        controlStateChangedHandler = async (args) => {
            await this.closeChat(this.chatId);
            this.chatId = undefined;
        }

        createChat(dashboardId, dashboardState) {
            const formData = new FormData();
            formData.append('dashboardId', dashboardId);
            formData.append('dashboardState', dashboardState);
            return this._tryFetch(async () => {
                const response = await fetch('/AIChat/CreateChat', {
                    method: 'POST',
                    body: formData
                });

                if(!response.ok) {
                    this._handleError({ code: `${response.status}`, message: `Internal server error` });
                    return;
                }
                return await response.text();
            }, 'CreateChat');
        }

        getAnswer(chatId, question) {
            const formData = new FormData();
            formData.append('chatId', chatId);
            formData.append('question', question);
            return this._tryFetch(async () => {
                const response = await fetch('/AIChat/GetAnswer', {
                    method: 'POST',
                    body: formData
                });

                if(!response.ok) {
                    this._handleError({ code: `${response.status}`, message: `Internal server error` });
                    return;
                }
                return await response.text();
            }, 'GetAnswer');
        }

        closeChat(chatId) {
            if (!chatId)
                return;

            const params = new URLSearchParams({ chatId });
            return this._tryFetch(async () => {
                await fetch(`/AIChat/CloseChat?${params}`, {
                    method: 'GET'
                });
            }, 'CloseAnswer');
        }

        async getAIResponse(question) {
            this.lastUserQuery = question;

            if(!this.chatId)
                this.chatId = await this.createChat(this.dashboardControl.getDashboardId(), this.dashboardControl.getDashboardState());
            if(this.chatId)
                return await this.getAnswer(this.chatId, question);
        };

        normalizeAIResponse(text) {
            text = text.replace(/【\d+:\d+†[^\】]+】/g, "");
            let html = marked.parse(text);
            if (/<p>\.\s*<\/p>\s*$/.test(html))
                html = html.replace(/<p>\.\s*<\/p>\s*$/, "")
            return html;
        }

        renderAssistantMessage(instance, message) {
            instance.option({ typingUsers: [] });
            instance.renderMessage({ timestamp: new Date(), text: message, author: assistant.name, id: assistant.id });
        }

        async refreshAnswer(instance) {
            const items = instance.option('items');
            const newItems = items.slice(0, -1);
            instance.option({ items: newItems });
            instance.option({ typingUsers: [assistant] });
            const aiResponse = await this.getAIResponse(this.lastUserQuery);
            setTimeout(() => {
                instance.option({ typingUsers: [] });
                this.renderAssistantMessage(instance, aiResponse);
            }, 200);
        }

        messageTemplate(data, $container) {
            const { message } = data;
            const container = $container.jquery ? $container.get(0) : $container;

            if (message.author.id && message.author.id !== assistant.id)
                return message.text;

            const textElement = document.createElement('div');
            textElement.innerHTML = this.normalizeAIResponse(message.text);
            container.appendChild(textElement);

            const buttonContainer = document.createElement('div');
            buttonContainer.classList.add('dx-bubble-button-container');
            this.lastRefreshButton?.remove();
            const copyBtnElement = document.createElement('div');
            new DevExpress.ui.dxButton(copyBtnElement, {
                icon: 'copy',
                stylingMode: 'text',
                onClick: () => navigator.clipboard.writeText(textElement.textContent)
            });
            buttonContainer.appendChild(copyBtnElement);
            const refreshBtnElement = document.createElement('div');
            new DevExpress.ui.dxButton(refreshBtnElement, {
                icon: 'refresh',
                stylingMode: 'text',
                onClick: () => this.refreshAnswer(data.component)
            });
            buttonContainer.appendChild(refreshBtnElement);
            this.lastRefreshButton = refreshBtnElement;
            container.appendChild(buttonContainer);
        }

        async onMessageEntered(e) {
            const instance = e.component;
            this.component.option('alerts', []);
            instance.renderMessage(e.message);
            instance.option({ typingUsers: [assistant] });
            const userInput = e.message.text + ((this.model.selectedSheet && "\nDiscuss item " + this.model.selectedSheet)
                || "\nLet's discuss all items");
            const response = await this.getAIResponse(userInput);
            this.renderAssistantMessage(instance, response);
        }

        renderContent($container, changeExisting) {
            const container = $container.jquery ? $container.get(0) : $container;
            const element = document.createElement('div');
            container.appendChild(element);

            this.component = new DevExpress.ui.dxChat(element, {
                messageTemplate: this.messageTemplate.bind(this),
                onMessageEntered: this.onMessageEntered.bind(this),
                showAvatar: false,
                showMessageTimestamp: false,
                showUserName: false,
                title: "AI Assistant",
                disabled: this.dashboardControl.isDesignMode(),
                user
            });
            this.disabledSubscription = this.dashboardControl.isDesignMode.subscribe(value => {
                this.component.option('disabled', value)
            });
        }
    }
    class AIChatCustomItem {
        constructor(dashboardControl) {
            dashboardControl.registerIcon(svgIcon);
            this.dashboardControl = dashboardControl;
            this.name = AI_CHAT_CUSTOM_ITEM;
            this.metaData = aiChatMetadata;
        }
   
        createViewerItem = (model, $element, options) => {
            return new AIChat(model, $element, options, this.dashboardControl);
        }
    }

    return AIChatCustomItem;
})();

window.AIChatItem = AIChatCustomItem;