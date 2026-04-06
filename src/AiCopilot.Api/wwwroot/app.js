(function () {
    const { createApp, nextTick } = Vue;

    createApp({
        data() {
            return {
                query: "",
                sessionId: null,
                loading: false,
                error: "",
                summary: "",
                recommendations: [],
                messages: []
            };
        },
        methods: {
            async sendMessage() {
                const trimmedQuery = this.query.trim();
                if (!trimmedQuery || this.loading) {
                    return;
                }

                this.error = "";
                this.loading = true;

                this.messages.push({
                    role: "user",
                    content: trimmedQuery
                });

                const pendingQuery = trimmedQuery;
                this.query = "";

                try {
                    const response = await fetch("/api/chat", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify({
                            sessionId: this.sessionId,
                            query: pendingQuery
                        })
                    });

                    if (!response.ok) {
                        throw new Error("The chat request could not be completed.");
                    }

                    const payload = await response.json();
                    this.sessionId = payload.sessionId;
                    this.summary = payload.summary || "";
                    this.recommendations = Array.isArray(payload.recommendations) ? payload.recommendations : [];

                    this.messages.push({
                        role: "assistant",
                        content: this.summary || "No summary was returned."
                    });

                    await nextTick();
                    window.scrollTo({ top: document.body.scrollHeight, behavior: "smooth" });
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "Unexpected error while calling chat.";
                    this.messages.push({
                        role: "assistant",
                        content: "I couldn't complete that request. Please try again once the API and backing services are available."
                    });
                } finally {
                    this.loading = false;
                }
            },
            formatScore(value) {
                if (typeof value !== "number") {
                    return "0.00";
                }

                return value.toFixed(2);
            }
        },
        template: `
            <main class="shell">
                <section class="hero">
                    <span class="eyebrow">Vue 3 Chat</span>
                    <h1>AI Copilot for grounded part guidance.</h1>
                    <p>Ask about parts, BOM structure, or recommendations. The interface keeps the current session, shows the latest response, and surfaces ranked recommendation cards from the chat API.</p>
                </section>

                <section class="workspace">
                    <div class="panel chat-panel">
                        <div class="chat-log">
                            <div v-if="messages.length === 0" class="empty-state">
                                <div>
                                    <strong>Start a conversation.</strong>
                                    <p>Try: "Recommend parts related to motor housing" or "Summarize BOM risks for PN-100."</p>
                                </div>
                            </div>

                            <article
                                v-for="(message, index) in messages"
                                :key="index"
                                class="message"
                                :class="message.role === 'user' ? 'message-user' : 'message-assistant'">
                                <span class="message-label">{{ message.role === 'user' ? 'You' : 'Copilot' }}</span>
                                <div>{{ message.content }}</div>
                            </article>
                        </div>

                        <div class="composer">
                            <form class="composer-form" @submit.prevent="sendMessage">
                                <textarea
                                    v-model="query"
                                    placeholder="Ask about parts, duplicates, BOM analysis, or recommendations..."
                                    :disabled="loading"></textarea>

                                <div class="composer-actions">
                                    <div class="status">
                                        <span v-if="loading">Generating response...</span>
                                        <span v-else-if="sessionId">Session: {{ sessionId }}</span>
                                        <span v-else>Ready for a new chat session.</span>
                                    </div>

                                    <button class="button" type="submit" :disabled="loading || !query.trim()">
                                        {{ loading ? 'Thinking...' : 'Send Message' }}
                                    </button>
                                </div>
                            </form>

                            <div v-if="error" class="error">{{ error }}</div>
                        </div>
                    </div>

                    <aside class="panel sidebar">
                        <div class="sidebar-header">
                            <h2>Recommendations</h2>
                            <p>The latest chat response summary and ranked related records appear here.</p>
                        </div>

                        <article v-if="summary" class="recommendation">
                            <div class="recommendation-top">
                                <h3 class="recommendation-title">Latest response</h3>
                            </div>
                            <p class="snippet">{{ summary }}</p>
                        </article>

                        <div v-if="recommendations.length > 0" class="recommendations">
                            <article v-for="item in recommendations" :key="item.embeddingId" class="recommendation">
                                <div class="recommendation-top">
                                    <h3 class="recommendation-title">{{ item.partNumber }} · {{ item.partName }}</h3>
                                    <span class="score-chip">Rank {{ formatScore(item.rankingScore) }}</span>
                                </div>
                                <div class="recommendation-meta">
                                    Similarity {{ formatScore(item.similarityScore) }} · {{ item.fileName }}
                                </div>
                                <p class="snippet">{{ item.snippet }}</p>
                            </article>
                        </div>

                        <div v-else class="empty-state" style="min-height: 220px;">
                            <div>
                                <strong>No recommendations yet.</strong>
                                <p>Once the API returns related parts, they’ll show up here as ranked cards.</p>
                            </div>
                        </div>
                    </aside>
                </section>
            </main>
        `
    }).mount("#app");
}());
