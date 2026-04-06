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
        computed: {
            dashboardRiskIndicators() {
                const items = this.recommendations.slice(0, 3);
                const fallbackSummary = this.summary || "No assessment yet.";

                if (items.length === 0) {
                    return [
                        {
                            label: "Overall risk",
                            status: "green",
                            state: "Stable",
                            detail: fallbackSummary
                        },
                        {
                            label: "Prediction confidence",
                            status: "yellow",
                            state: "Waiting",
                            detail: "Ask a question to generate live recommendations and risk signals."
                        },
                        {
                            label: "Action urgency",
                            status: "green",
                            state: "Normal",
                            detail: "No immediate actions detected."
                        }
                    ];
                }

                return items.map((item, index) => {
                    const risk = this.getRiskState(item);
                    const labels = ["Overall risk", "Prediction confidence", "Action urgency"];
                    return {
                        label: labels[index] || `Indicator ${index + 1}`,
                        status: risk.color,
                        state: risk.label,
                        detail: `${item.partNumber} · rank ${this.formatScore(item.rankingScore)} · similarity ${this.formatScore(item.similarityScore)}`
                    };
                });
            },
            dashboardPredictions() {
                const items = this.recommendations.slice(0, 3);

                if (items.length === 0) {
                    return [
                        {
                            title: "No predictions yet",
                            value: "Waiting for chat results",
                            detail: "Predictions will populate from the most recent grounded response."
                        }
                    ];
                }

                return items.map((item) => ({
                    title: `${item.partNumber} forecast`,
                    value: this.getPredictionLabel(item),
                    detail: `${item.partName} appears ${this.getPredictionTone(item)} based on the returned ranking and similarity signals.`
                }));
            },
            dashboardSuggestions() {
                const items = this.recommendations.slice(0, 3);

                if (items.length === 0) {
                    return [
                        "Start with a part question or BOM analysis request to generate grounded suggestions.",
                        "Use specific part numbers for sharper recommendations.",
                        "Follow up in the same session to keep context connected."
                    ];
                }

                return items.map((item) => this.getSuggestion(item));
            }
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
            },
            getRiskState(item) {
                if (item.rankingScore >= 0.85 || item.similarityScore >= 0.9) {
                    return {
                        label: "High focus",
                        color: "red"
                    };
                }

                if (item.rankingScore >= 0.6 || item.similarityScore >= 0.75) {
                    return {
                        label: "Watch",
                        color: "yellow"
                    };
                }

                return {
                    label: "Stable",
                    color: "green"
                };
            },
            getPredictionLabel(item) {
                if (item.rankingScore >= 0.85) {
                    return "Potential duplicate or high relevance";
                }

                if (item.rankingScore >= 0.6) {
                    return "Needs review";
                }

                return "Low concern";
            },
            getPredictionTone(item) {
                if (item.similarityScore >= 0.9) {
                    return "strongly related";
                }

                if (item.similarityScore >= 0.75) {
                    return "possibly related";
                }

                return "lower priority";
            },
            getSuggestion(item) {
                if (item.rankingScore >= 0.85) {
                    return `Review ${item.partNumber} first and confirm whether ${item.partName} should be merged, blocked, or escalated.`;
                }

                if (item.rankingScore >= 0.6) {
                    return `Inspect ${item.partNumber} with a targeted follow-up query to validate the recommendation before action.`;
                }

                return `Keep ${item.partNumber} as a background candidate and prioritize higher-ranked recommendations first.`;
            }
        },
        template: `
            <main class="shell">
                <section class="hero">
                    <span class="eyebrow">Vue 3 Chat</span>
                    <h1>AI Copilot for grounded part guidance.</h1>
                    <p>Ask about parts, BOM structure, or recommendations. The interface keeps the current session, shows the latest response, and surfaces ranked recommendation cards from the chat API.</p>
                </section>

                <section class="dashboard">
                    <article class="panel dashboard-panel">
                        <div class="dashboard-header">
                            <h2>Risk indicators</h2>
                            <p>Traffic-light guidance based on the latest recommendation signals.</p>
                        </div>
                        <div class="risk-grid">
                            <div v-for="indicator in dashboardRiskIndicators" :key="indicator.label" class="risk-card">
                                <div class="risk-top">
                                    <span class="risk-dot" :class="'risk-' + indicator.status"></span>
                                    <strong>{{ indicator.label }}</strong>
                                </div>
                                <div class="risk-state">{{ indicator.state }}</div>
                                <div class="risk-detail">{{ indicator.detail }}</div>
                            </div>
                        </div>
                    </article>

                    <article class="panel dashboard-panel">
                        <div class="dashboard-header">
                            <h2>Predictions</h2>
                            <p>Quick rule-of-thumb interpretations from the current result set.</p>
                        </div>
                        <div class="prediction-list">
                            <div v-for="prediction in dashboardPredictions" :key="prediction.title" class="prediction-card">
                                <div class="prediction-title">{{ prediction.title }}</div>
                                <div class="prediction-value">{{ prediction.value }}</div>
                                <div class="prediction-detail">{{ prediction.detail }}</div>
                            </div>
                        </div>
                    </article>

                    <article class="panel dashboard-panel">
                        <div class="dashboard-header">
                            <h2>Suggestions</h2>
                            <p>Recommended next moves based on the top ranked outputs.</p>
                        </div>
                        <ul class="suggestion-list">
                            <li v-for="suggestion in dashboardSuggestions" :key="suggestion">{{ suggestion }}</li>
                        </ul>
                    </article>
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
