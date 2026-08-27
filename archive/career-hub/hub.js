/**
 * Career Hub static prototype — shared interactions (vanilla JS, no network).
 */
(function () {
  "use strict";

  var MONTHS = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
  ];
  var DAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  var WORLD_FEED = [
    { tag: "projection", label: "Public result", text: "VeloStar win stage 4 at UAE Tour — world continues without your squad." },
    { tag: "notification", label: "Scout report", text: "Scout report arrived: U23 climber dossier updated (staff interpretation, not ground truth)." },
    { tag: "rumor", label: "Transfer rumor", text: "World transfer rumor: sprinter linked to continental move — unverified, agent-sourced." },
    { tag: "projection", label: "Public result", text: "Paris–Nice organiser published start list addendum (public calendar data)." },
    { tag: "notification", label: "Notification", text: "Medical department logged routine screening completions — presentation only." }
  ];

  var DETAIL_DATA = {
    "sponsor-search": {
      title: "Sponsor search inactive",
      blocks: [
        { tag: "STATE", text: "No active sponsor search process is running." },
        { tag: "WHY", text: "Commercial Director Marc Dupont has not launched a new search cycle since the January board review." },
        { tag: "FORECAST", text: "Without action, primary sponsor slot risk becomes critical by 18 Mar 2027." }
      ],
      cta: "Assign sponsor search — prototype",
      note: "Stand-in for an Application Command. Responsible owner: Commercial Director."
    },
    "recruitment-load": {
      title: "Recruitment workload high",
      blocks: [
        { tag: "STATE", text: "Department load at 86% capacity." },
        { tag: "WHY", text: "4 active cases: 2 rider renewals, 1 incoming target, 1 outbound enquiry." },
        { tag: "FORECAST", text: "Opening another case would push load past 95% — response times slip and cases queue." }
      ],
      cta: "Open Recruitment workload — prototype",
      note: "Stand-in for an Application Command. Head of Recruitment owns throughput."
    },
    "paris-nice": {
      title: "Paris–Nice — race briefing",
      blocks: [
        { tag: "STATE", text: "Race starts in 2 days. Pre-race briefing is not confirmed." },
        { tag: "WHY", text: "Calendar commitment registered 12 Feb; Sporting Director owes roster + tactics briefing before departure." },
        { tag: "FORECAST", text: "Unconfirmed briefing blocks clean Advance Day on race eve (Decision Request)." }
      ],
      cta: "Confirm race briefing — prototype",
      note: "Deadline lives in calendar system — Inbox only presents it."
    }
  };

  var state = {
    currentDate: null,
    feedIndex: 0,
    parisNiceDays: 2,
    recruitmentLoad: 86,
    decisionPending: false,
    decisionResolved: false,
    decisionInterrupt: false
  };

  function parseStartDate() {
    var raw = document.body.getAttribute("data-start-date") || "2027-03-05";
    var parts = raw.split("-");
    return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
  }

  function formatDate(d) {
    return DAYS[d.getDay()] + " " + d.getDate() + " " + MONTHS[d.getMonth()] + " " + d.getFullYear();
  }

  function formatFeedTime(d) {
    return formatDate(d) + " · projection timestamp";
  }

  function $(sel, root) {
    return (root || document).querySelector(sel);
  }

  function $all(sel, root) {
    return Array.prototype.slice.call((root || document).querySelectorAll(sel));
  }

  function showToast(message) {
    var container = $("#toast-container");
    if (!container) {
      container = document.createElement("div");
      container.id = "toast-container";
      container.className = "toast-container";
      document.body.appendChild(container);
    }
    var el = document.createElement("div");
    el.className = "toast";
    el.textContent = message;
    container.appendChild(el);
    setTimeout(function () {
      el.remove();
    }, 3200);
  }

  function updateDateDisplay() {
    var el = $("#world-date");
    if (el) {
      el.textContent = formatDate(state.currentDate);
    }
  }

  function prependFeedItem(html) {
    var list = $("#feed-list");
    if (!list) return;
    var li = document.createElement("li");
    li.className = "feed-item";
    li.innerHTML = html;
    list.insertBefore(li, list.firstChild);
  }

  function addWorldFeedEvent() {
    var item = WORLD_FEED[state.feedIndex % WORLD_FEED.length];
    state.feedIndex += 1;
    var html =
      '<span class="feed-tag ' + item.tag + '">' + item.label + "</span>" +
      item.text +
      "<time>" + formatFeedTime(state.currentDate) + "</time>";
    prependFeedItem(html);
  }

  function shiftEmployedState() {
    if (state.parisNiceDays > 0) {
      state.parisNiceDays -= 1;
      var deadlineEl = $("#deadline-days");
      if (deadlineEl) {
        if (state.parisNiceDays === 0) {
          deadlineEl.textContent = "Starts today";
        } else if (state.parisNiceDays === 1) {
          deadlineEl.textContent = "Starts in 1 day";
        } else {
          deadlineEl.textContent = "Starts in " + state.parisNiceDays + " days";
        }
      }
    }
    if (state.recruitmentLoad < 92) {
      state.recruitmentLoad += 1;
      var loadEl = $("#recruitment-load-pct");
      if (loadEl) {
        loadEl.textContent = state.recruitmentLoad + "%";
      }
    }
  }

  function advanceOneDay() {
    state.currentDate.setDate(state.currentDate.getDate() + 1);
    updateDateDisplay();
    addWorldFeedEvent();
    if (document.body.getAttribute("data-variant") === "employed") {
      shiftEmployedState();
    }
    showToast("Day advanced — prototype simulation step.");
  }

  function openDrawer(key) {
    var data = DETAIL_DATA[key];
    if (!data) return;
    var overlay = $("#drawer-overlay");
    var drawer = $("#detail-drawer");
    if (!overlay || !drawer) return;

    $("#drawer-title").textContent = data.title;
    var body = $("#drawer-body-content");
    body.innerHTML = data.blocks
      .map(function (b) {
        return (
          '<div class="swf-block"><span class="swf-tag">' + b.tag +
          '</span><p>' + b.text + "</p></div>"
        );
      })
      .join("");
    $("#drawer-cta").textContent = data.cta;
    $("#drawer-note").textContent = data.note;

    overlay.classList.add("is-open");
    drawer.classList.add("is-open");
  }

  function closeDrawer() {
    var overlay = $("#drawer-overlay");
    var drawer = $("#detail-drawer");
    if (overlay) overlay.classList.remove("is-open");
    if (drawer) drawer.classList.remove("is-open");
  }

  function openDecisionModal() {
    var modal = $("#decision-modal");
    if (modal) modal.classList.add("is-open");
  }

  function closeDecisionModal() {
    var modal = $("#decision-modal");
    if (modal) modal.classList.remove("is-open");
  }

  function confirmBriefing() {
    closeDecisionModal();
    state.decisionPending = false;
    state.decisionResolved = true;
    prependFeedItem(
      '<span class="feed-tag notification">Hub note</span>' +
      "Briefing confirmed — prototype." +
      "<time>" + formatFeedTime(state.currentDate) + "</time>"
    );
    showToast("Briefing confirmed. You may Advance Day again.");
  }

  function handleAdvanceDay() {
    if (state.decisionInterrupt && !state.decisionResolved) {
      state.decisionPending = true;
      openDecisionModal();
      return;
    }
    advanceOneDay();
  }

  function handleDrawerCta() {
    showToast("Prototype: Application Command not wired — " + $("#drawer-cta").textContent);
    closeDrawer();
  }

  function toggleInboxPanel() {
    var panel = $("#inbox-local-panel");
    if (panel) {
      panel.classList.toggle("hidden");
      if (!panel.classList.contains("hidden")) {
        panel.scrollIntoView({ behavior: "smooth", block: "nearest" });
      }
    } else {
      showToast("Inbox opens presentation of pending items — not a mail database (prototype).");
    }
  }

  function bindNav() {
    $all("[data-nav]").forEach(function (el) {
      el.addEventListener("click", function (e) {
        e.preventDefault();
        var target = el.getAttribute("data-nav");
        if (target === "inbox") {
          toggleInboxPanel();
          return;
        }
        if (target === "hq") return;
        if (el.getAttribute("data-unavailable") === "unemployed") {
          showToast("Not available while unemployed — no org AccessContext (prototype).");
          return;
        }
        showToast("Prototype: this screen is not built");
      });
    });
  }

  function bindDetailCards() {
    $all("[data-detail]").forEach(function (el) {
      el.addEventListener("click", function () {
        openDrawer(el.getAttribute("data-detail"));
      });
    });
  }

  function bindDrawer() {
    var closeBtn = $("#drawer-close");
    var overlay = $("#drawer-overlay");
    var cta = $("#drawer-cta");
    if (closeBtn) closeBtn.addEventListener("click", closeDrawer);
    if (overlay) overlay.addEventListener("click", closeDrawer);
    if (cta) cta.addEventListener("click", handleDrawerCta);
  }

  function bindModal() {
    var confirmBtn = $("#modal-confirm");
    var inboxBtn = $("#modal-inbox");
    var dismissBtn = $("#modal-dismiss");

    if (confirmBtn) {
      confirmBtn.addEventListener("click", confirmBriefing);
    }
    if (inboxBtn) {
      inboxBtn.addEventListener("click", function () {
        closeDecisionModal();
        toggleInboxPanel();
        showToast("Decision still pending — confirm briefing to advance.");
      });
    }
    if (dismissBtn) {
      dismissBtn.addEventListener("click", function () {
        closeDecisionModal();
        showToast("Decision Request still pending.");
      });
    }
  }

  function bindStaffDismiss() {
    $all(".btn-dismiss-rec").forEach(function (btn) {
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var rec = btn.closest(".staff-rec");
        if (rec) {
          rec.style.opacity = "0.45";
          rec.querySelector(".rec-text").textContent += " (dismissed visually — prototype only)";
          btn.disabled = true;
        }
      });
    });
  }

  function bindJobCards() {
    $all(".job-card .btn-prototype").forEach(function (btn) {
      btn.addEventListener("click", function () {
        showToast("Prototype: job application flow not built.");
      });
    });
  }

  function init() {
    state.currentDate = parseStartDate();
    state.decisionInterrupt = document.body.getAttribute("data-decision-interrupt") === "true";
    updateDateDisplay();

    var advanceBtn = $("#advance-day");
    if (advanceBtn) {
      advanceBtn.addEventListener("click", handleAdvanceDay);
    }

    bindNav();
    bindDetailCards();
    bindDrawer();
    bindModal();
    bindStaffDismiss();
    bindJobCards();

    var inboxTop = $("#inbox-top-btn");
    if (inboxTop) {
      inboxTop.addEventListener("click", toggleInboxPanel);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
