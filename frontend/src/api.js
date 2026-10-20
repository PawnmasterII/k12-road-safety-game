const TOKEN_KEY = "cutr_token";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

export async function api(method, path, body = null) {
  const headers = { "Content-Type": "application/json" };
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  const res = await fetch(`/api${path}`, {
    method,
    headers,
    body: body === null ? undefined : JSON.stringify(body),
  });
  let data = null;
  try {
    data = await res.json();
  } catch {
    data = {};
  }
  if (!res.ok) {
    // detail can be a string (HTTPException) OR an object/array (FastAPI 422
    // validation errors) — putting a non-string in new Error() renders
    // "[object Object]" to kids. Stringify any non-string detail.
    const raw = data.detail;
    const detail = typeof raw === "string"
      ? raw
      : (raw == null ? "" : JSON.stringify(raw));
    const err = new Error(detail || "Something went wrong. Try again!");
    err.status = res.status;
    throw err;
  }
  return data;
}
