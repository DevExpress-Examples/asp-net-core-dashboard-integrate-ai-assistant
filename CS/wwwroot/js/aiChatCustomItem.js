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
        constructor(model, $container, options, dashboardControl, chatModel) {
            super(model, $container, options);

            this.lastRefreshButton = undefined;
            this.model.selectedSheet = undefined;
            this.model.updateChatItem = undefined;
            this.component = undefined;
            this.dashboardControl = dashboardControl;
            this.disabledSubscription = undefined;
            this.chatModel = chatModel;
            this.dashboardControl.on('dashboardInitialized', this.controlStateChangedHandler);
            this.dashboardControl.on('dashboardStateChanged', this.controlStateChangedHandler);
        }
        dispose() {
            super.dispose();
            this.disabledSubscription.dispose();
            this.dashboardControl.off('dashboardInitialized', this.controlStateChangedHandler);
            this.dashboardControl.off('dashboardStateChanged', this.controlStateChangedHandler);
        }
        
        controlStateChangedHandler = async (args) => {
            await this.chatModel.closeChat();
        }

        normalizeAIResponse(text) {
            if (text) {
                text = text.replace(/【\d+:\d+†[^\】]+】/g, "");
                let html = marked.parse(text);
                if (/<p>\.\s*<\/p>\s*$/.test(html))
                    html = html.replace(/<p>\.\s*<\/p>\s*$/, "")
                return html;
            }
            else {
                return "Please try again later."
            }
        }

        renderAssistantMessage(message) {
            this.component.option({ typingUsers: [] });
            this.component.renderMessage({ timestamp: new Date(), text: message, author: assistant.name, id: assistant.id });
        }
        alertErrors(errorList) {
            this.component.option('alerts', errorList)
        }
        getMessageHistory() {
            return this.component.option('items');
        }

        async refreshAnswer(instance) {
            const items = instance.option('items');
            const newItems = items.slice(0, -1);
            instance.option({ items: newItems });
            instance.option({ typingUsers: [assistant] });
            await this.chatModel.reloadLastRequest();
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
            if(data.component.option('items').at(-1).author === assistant.name) {
                buttonContainer.appendChild(refreshBtnElement);
                this.lastRefreshButton = refreshBtnElement;
            }
            container.appendChild(buttonContainer);
        }

        async onMessageEntered(e) {
            this.lastRefreshButton?.remove();
            const instance = e.component;
            this.component.option('alerts', []);
            instance.renderMessage(e.message);
            instance.option({ typingUsers: [assistant] });
            const userInput = e.message.text + ((this.model.selectedSheet && "\nDiscuss item " + this.model.selectedSheet)
                || "\nLet's discuss all items");
            await this.chatModel.getAIResponse(userInput);
        }

        renderContent($container) {
            const state = this.chatModel.getState();
            if(this.component) {
                this.component.option(state);
            } else {
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
                    user,
                    ...state
                });
                
                this.disabledSubscription = this.dashboardControl.isDesignMode.subscribe(value => {
                    this.component.option('disabled', value)
                });
            }
            this.chatModel.currentViewItem = this;
        }
    }
    class AIChatCustomItem {
        constructor(dashboardControl) {
            dashboardControl.registerIcon(svgIcon);
            this.dashboardControl = dashboardControl;
            this.name = AI_CHAT_CUSTOM_ITEM;
            this.metaData = aiChatMetadata;
            this.chatId = '';
            this.lastUserQuery = '';
            this.errorList = [];
            this.isLoading = false;
            this.currentViewItem = null;
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
                this.currentViewItem?.alertErrors(this.errorList);
            }, 10000);
            this.errorList.push({
                id: id,
                message: `${error.code} - ${error.message}`
            });
            this.currentViewItem?.alertErrors(this.errorList);
        }

        async tryCreateChat() {
            const formData = new FormData();
            formData.append('dashboardId', this.dashboardControl.getDashboardId());
            formData.append('dashboardState', this.dashboardControl.getDashboardState());
            this.chatId = await this._tryFetch(async () => {
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
            return !!this.chatId;
        }

        async getAnswer(question) {
            const formData = new FormData();
            formData.append('chatId', this.chatId);
            formData.append('question', question);
            try {
                return await this._tryFetch(async () => {
                    const response = await fetch('/AIChat/GetAnswer', {
                        method: 'POST',
                        body: formData
                    });

                    if (!response.ok) {
                        this._handleError({ code: `${response.status}`, message: `Internal server error` });
                        return;
                    }
                    return await response.text();
                }, 'GetAnswer');
            } finally {
                this.isLoading = false;
            }
        }

        async closeChat() {
            if (!this.chatId)
                return;

            const params = new URLSearchParams({ chatId: this.chatId });
            try{
                await this._tryFetch(async () => {
                    await fetch(`/AIChat/CloseChat?${params}`, {
                        method: 'GET'
                    });
                }, 'CloseAnswer');
            } finally {
                this.chatId = '';
            }
        }

        async getAIResponse(question) {
            this.lastUserQuery = question;
            this.isLoading = true;

            if(!this.chatId && !await this.tryCreateChat())
                return;
            const answer = await this.getAnswer(question);
            this.currentViewItem?.renderAssistantMessage(answer);
        };

        async reloadLastRequest() {
            if(!this.lastUserQuery)
                return;
            await this.getAIResponse(this.lastUserQuery);
        }

        getState() {
            return {
                items: this.currentViewItem?.getMessageHistory() || [],
                typingUsers: this.isLoading ? [assistant] : []
            }
        }

        createViewerItem = (model, $element, options) => {
            return new AIChat(model, $element, options, this.dashboardControl, this);
        }
    }

    return AIChatCustomItem;
})();

window.AIChatItem = AIChatCustomItem;