"use client";

import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useI18n } from "@/i18n/I18nProvider";
import * as waService from "@/services/whatsapp.service";
import type { WhatsAppMessage } from "@/types/sequences";

export default function WhatsAppPage() {
  const { t } = useI18n();
  const [phone, setPhone] = useState("");
  const [activePhone, setActivePhone] = useState("");
  const [body, setBody] = useState("");
  const [sendError, setSendError] = useState("");
  const [optMsg, setOptMsg] = useState("");

  const { data: messages = [], isLoading, refetch } = useQuery({
    queryKey: ["wa-conversation", activePhone],
    queryFn: ({ signal }) => (activePhone ? waService.getConversation(activePhone, 1, 50, signal) : Promise.resolve<WhatsAppMessage[]>([])),
    enabled: !!activePhone,
  });

  const sendMutation = useMutation({
    mutationFn: ({ to, msg }: { to: string; msg: string }) => waService.sendWhatsApp(to, msg),
    onSuccess: () => { setBody(""); setSendError(""); refetch(); },
    onError: (err: Error) => setSendError(err.message),
  });

  const optInMutation = useMutation({
    mutationFn: (p: string) => waService.optIn(p),
    onSuccess: () => { setOptMsg(t("whatsapp.optIn") + " ✓"); setTimeout(() => setOptMsg(""), 3000); },
    onError: (err: Error) => setOptMsg(err.message),
  });

  const optOutMutation = useMutation({
    mutationFn: (p: string) => waService.optOut(p),
    onSuccess: () => { setOptMsg(t("whatsapp.optOut") + " ✓"); setTimeout(() => setOptMsg(""), 3000); },
    onError: (err: Error) => setOptMsg(err.message),
  });

  return (
    <main className="p-6 max-w-3xl mx-auto">
      <div className="mb-4">
        <h1 className="text-2xl font-bold">{t("whatsapp.title")}</h1>
        <p className="text-gray-500 text-sm">{t("whatsapp.subtitle")}</p>
      </div>

      {/* Phone lookup */}
      <div className="flex gap-2 mb-4">
        <input
          className="input flex-1"
          placeholder={t("whatsapp.phone")}
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
        />
        <button
          className="btn btn-primary"
          onClick={() => setActivePhone(phone.trim())}
          disabled={!phone.trim()}
        >
          {t("whatsapp.conversation")}
        </button>
        <button
          className="btn btn-secondary"
          onClick={() => optInMutation.mutate(phone.trim())}
          disabled={!phone.trim()}
          title={t("whatsapp.optIn")}
        >
          Opt In
        </button>
        <button
          className="btn btn-secondary"
          onClick={() => optOutMutation.mutate(phone.trim())}
          disabled={!phone.trim()}
          title={t("whatsapp.optOut")}
        >
          Opt Out
        </button>
      </div>
      {optMsg && <p className="text-sm text-green-600 mb-2">{optMsg}</p>}

      {/* Conversation */}
      {activePhone && (
        <>
          <div className="border rounded p-3 mb-3 h-80 overflow-y-auto bg-gray-50 space-y-2">
            {isLoading && <p className="text-gray-400 text-sm">{t("common.loading")}</p>}
            {!isLoading && messages.length === 0 && (
              <p className="text-gray-400 text-sm">{t("whatsapp.empty")}</p>
            )}
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex ${msg.direction === "outbound" ? "justify-end" : "justify-start"}`}
              >
                <div
                  className={`max-w-[70%] px-3 py-2 rounded-lg text-sm ${
                    msg.direction === "outbound"
                      ? "bg-blue-600 text-white"
                      : "bg-white border text-gray-800"
                  }`}
                >
                  <p>{msg.body}</p>
                  <p className="text-xs opacity-60 mt-0.5">
                    {new Date(msg.sentAtUtc).toLocaleTimeString()} — {msg.status}
                  </p>
                </div>
              </div>
            ))}
          </div>

          {/* Send form */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              if (body.trim()) sendMutation.mutate({ to: activePhone, msg: body.trim() });
            }}
            className="flex gap-2"
          >
            <input
              className="input flex-1"
              placeholder={t("whatsapp.body")}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              maxLength={4096}
            />
            <button type="submit" className="btn btn-primary" disabled={!body.trim()}>
              {t("whatsapp.send")}
            </button>
          </form>
          {sendError && <p className="text-red-500 text-sm mt-1">{sendError}</p>}
        </>
      )}
    </main>
  );
}
