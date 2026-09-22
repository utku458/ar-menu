// Generated from contracts/openapi/v1.json by `pnpm generate:api`. Do not edit.

export interface paths {
    "/api/v1/menus/{tenant}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The guest-facing menu opened from a QR code, in the best language available. */
        get: {
            parameters: {
                query?: {
                    /** @description Explicit language choice, e.g. from a language switcher. Wins over Accept-Language. */
                    lang?: string;
                };
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["PublicMenuResponse"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/menus/{tenant}/events": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Counts guest events on the menu: views, opened dishes, loaded models, AR starts. No guest identifier is accepted or stored. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["MenuEventsRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/statistics": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Daily guest activity and the dishes guests open most, over the last days (7 by default, at most 90). */
        get: {
            parameters: {
                query?: {
                    days?: number;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["MenuStatisticsResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/history": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Who changed what in the business and when, newest first. Pass nextCursor as before for the next page. */
        get: {
            parameters: {
                query?: {
                    before?: string;
                    limit?: number;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["HistoryPage"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/me/workspaces": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The businesses the signed-in person belongs to, with their role in each. Each needs its own sign-in. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["WorkspaceResponse"][];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/me/email-verification": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** E-mails the signed-in user a new verification link, unless the address is already verified. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ResendVerificationRequest"];
                };
            };
            responses: {
                /** @description Accepted */
                202: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/me/password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Replaces the signed-in person's password, confirmed with the current one. Every session ends, this one included. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ChangePasswordRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/me/deletion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** What deleting the account would do: businesses that close with it, must be handed over first, or are left. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["AccountDeletionResponse"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        /** Deletes the account for good, confirmed with the password. Refused while the person owns a business with other members. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["DeleteAccountRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/auth/sign-in": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Signs a user-name account in to its own business, or the administrator to the platform; says which. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["UserNameSignInRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["WorkspaceAccessTokenResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The signed-in member and the tenant the access token is scoped to. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["CurrentUserResponse"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Members of the business and the invitations waiting for an answer. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["TeamResponse"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/invitations": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Invites someone by e-mail. The invitation is saved even when the e-mail cannot be delivered; emailSent tells. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["InvitationRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["InvitationSent"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/invitations/{invitationId}/resend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sends a pending invitation again with a new link and deadline; the previous link stops working. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    invitationId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ResendRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["InvitationSent"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/invitations/{invitationId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Withdraws a pending invitation. */
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    invitationId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/members": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Opens an account that signs in with a user name and password, and adds it to the team. No e-mail is sent. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["AddMemberRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["MemberAddedResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/members/{membershipId}/password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Gives a user-name member a new password and ends their sessions. E-mail accounts reset their own. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    membershipId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["MemberPasswordRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/members/{membershipId}/role": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Moves a member between manager and staff, from their next access token on. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    membershipId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["RoleRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/members/{membershipId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Removes a member and ends their sessions. The owner cannot be removed. */
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    membershipId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/team/members/{membershipId}/ownership": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Hands the business over to a member, confirmed with the owner's password. The owner becomes a manager. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    membershipId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["PasswordConfirmationRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/tenants/{tenant}/invitations/lookup": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** What an invitation link offers: the business, the role, the address, and whether it has an account. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["InvitationTokenRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["InvitationResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/tenants/{tenant}/invitations/accept": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Joins the team with an invitation link and signs in; the refresh token is set as an HttpOnly cookie. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["AcceptInvitationRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["AccessTokenResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/settings/languages": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Sets the languages the menu is offered in and the default one guests fall back to. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["LanguagesRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/settings/branding": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Sets the business's name, the logo above its menu and the colour the menu is painted with. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["BrandingRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/settings/time-zone": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Sets the business's IANA time zone, where its days begin. Days already counted keep their boundaries. */
        put: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["TimeZoneRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/platform/businesses": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Every business on the platform, by name. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["BusinessSummary"][];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        /** Opens a business with its owner's account, who signs in with the given user name and password. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["OpenBusinessRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["OpenedBusinessResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/platform/businesses/{businessId}/enter": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** An access token for the business with the owner's role. Short-lived and not refreshable: enter again when it expires. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    businessId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["EnteredBusinessResponse"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["ManagedMenuResponse"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/items/{itemId}/availability": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["AvailabilityRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/categories": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CategoryRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["CreatedResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/categories/order": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ReorderRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/categories/{categoryId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    categoryId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CategoryRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    categoryId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/categories/{categoryId}/items/order": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    categoryId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ReorderRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/items": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["CreateItemRequest"];
                };
            };
            responses: {
                /** @description Created */
                201: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["CreatedResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/items/{itemId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["UpdateItemRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/items/{itemId}/ar-model": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ArModelRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        post?: never;
        delete: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/export": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The whole menu as a CSV spreadsheet (UTF-8), one row per dish and one column per language. */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "text/csv": string;
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/import": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Applies a menu spreadsheet in the export's format: rows with an id update that dish, rows without one add a dish. All or nothing; with dryRun=true only reports what would change. */
        post: {
            parameters: {
                query?: {
                    dryRun?: boolean;
                };
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "text/csv": string;
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["MenuImportResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Payload Too Large */
                413: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Unsupported Media Type */
                415: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/menu/items/{itemId}/ar-model/processing": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** The latest model processing of the item: its progress, or its report once finished. */
        get: operations["GetArModelProcessing"];
        put?: never;
        /** Turns an uploaded GLB into the item's 3D model: an optimized GLB, a Scene Viewer GLB, a USDZ and a poster. The item keeps its current model until processing succeeds. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    itemId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["ArModelProcessingRequest"];
                };
            };
            responses: {
                /** @description Accepted */
                202: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["ArModelProcessingResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/auth/password-reset": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** E-mails a password reset link if the address has an account. Always 202, whether or not it has one. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["PasswordResetRequest"];
                };
            };
            responses: {
                /** @description Accepted */
                202: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/auth/password-reset/confirm": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sets a new password with a reset link. Existing sessions of the account can no longer be refreshed. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["NewPasswordRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/auth/email-verification/confirm": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Confirms the account's e-mail address with the link sent to it. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["EmailVerificationRequest"];
                };
            };
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Conflict */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/tenants/{tenant}/auth/sign-in": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Signs a member in and returns an access token; the refresh token is set as an HttpOnly cookie. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["SignInRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["AccessTokenResponse"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/tenants/{tenant}/auth/refresh": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Rotates the refresh token cookie and returns a new access token. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["AccessTokenResponse"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/tenants/{tenant}/auth/sign-out": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Ends the session and clears the refresh token cookie. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    /** @description Slug of the business, as printed in its QR code links. */
                    tenant: string;
                };
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description No Content */
                204: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Too Many Requests */
                429: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/assets/uploads": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Grants a short-lived upload of exactly one file, of the declared type and size, straight to storage. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["UploadRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["AssetUpload"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/v1/manage/assets/uploads/{uploadId}/publish": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Inspects an uploaded file and publishes it under an immutable public URL, ready to attach to an item. */
        post: {
            parameters: {
                query?: never;
                header?: never;
                path: {
                    uploadId: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["PublishRequest"];
                };
            };
            responses: {
                /** @description OK */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["PublishedAsset"];
                    };
                };
                /** @description Bad Request */
                400: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                    };
                };
                /** @description Unauthorized */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Forbidden */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
                /** @description Not Found */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["ProblemDetails"];
                    };
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        AcceptInvitationRequest: {
            token: string;
            fullName: null | string;
            password: string;
        };
        AccessTokenResponse: {
            accessToken: string;
            /** Format: date-time */
            expiresAt: string;
            /** Format: int32 */
            expiresIn: number;
            tokenType?: null | string;
        };
        AccountDeletionResponse: {
            closingWorkspaces: components["schemas"]["AffectedWorkspace"][];
            workspacesToHandOver: components["schemas"]["AffectedWorkspace"][];
            workspacesToLeave: components["schemas"]["AffectedWorkspace"][];
        };
        AddMemberRequest: {
            fullName: string;
            userName: string;
            password: string;
            role: components["schemas"]["TeamRole"];
        };
        AffectedWorkspace: {
            slug: string;
            name: string;
            /** Format: int32 */
            otherMembers: number;
        };
        ArModelProcessingRequest: {
            /** Format: uuid */
            uploadId: string;
        };
        ArModelProcessingResponse: {
            /** Format: uuid */
            id: string;
            status: components["schemas"]["ArModelProcessingState"];
            /** Format: int32 */
            attempts: number;
            failureCode: null | string;
            report: null | components["schemas"]["ArModelReportResponse"];
            /** Format: date-time */
            createdAt: string;
            /** Format: date-time */
            completedAt: null | string;
        };
        /** @enum {unknown} */
        ArModelProcessingState: "queued" | "processing" | "succeeded" | "failed" | "superseded";
        ArModelReportResponse: {
            source: components["schemas"]["ModelStatisticsResponse"];
            optimized: components["schemas"]["ModelStatisticsResponse"];
            files: components["schemas"]["ModelFileSizesResponse"];
            dimensions: components["schemas"]["ModelDimensionsResponse"];
            warnings: string[];
        };
        ArModelRequest: {
            glbPath: string;
            sceneViewerGlbPath: null | string;
            usdzPath: null | string;
            posterPath: null | string;
        };
        /**
         * @description Everything &lt;model-viewer&gt; needs: `src`, `ios-src` and `poster`, plus the plain GLB that devices
         *     opening Android Scene Viewer should load instead of Uri ArModelResponse.GlbUrl.
         */
        ArModelResponse: {
            /** Format: uri */
            glbUrl: string;
            /** Format: uri */
            sceneViewerGlbUrl: null | string;
            /** Format: uri */
            usdzUrl: null | string;
            /** Format: uri */
            posterUrl: null | string;
        };
        /**
         * @description What a browser may upload: the files of a dish's 3D/AR model (see `ArModel`), or a business's logo.
         * @enum {unknown}
         */
        AssetKind: "model" | "appleModel" | "poster" | "logo";
        AssetUpload: {
            /** Format: uuid */
            uploadId: string;
            /** Format: uri */
            url: string;
            method: string;
            headers: {
                [key: string]: string;
            };
            /** Format: date-time */
            expiresAt: string;
        };
        AvailabilityRequest: {
            isAvailable: boolean;
        };
        /**
         * @description `LogoPath` is the `path` of a published logo upload, or `null` to show no logo; `AccentColor`
         *     is written `#rrggbb`, or `null` for the platform's own neutral style.
         */
        BrandingRequest: {
            name: string;
            logoPath: null | string;
            accentColor: null | string;
        };
        BusinessSummary: {
            /** Format: uuid */
            id: string;
            name: string;
            slug: string;
            status: string;
            /** Format: date-time */
            createdAt: string;
        };
        CategoryRequest: {
            name: {
                [key: string]: string;
            };
            description: null | {
                [key: string]: string;
            };
            /** @default true */
            isVisible: boolean;
        };
        ChangePasswordRequest: {
            currentPassword: string;
            newPassword: string;
        };
        CreatedResponse: {
            /** Format: uuid */
            id: string;
        };
        /**
         * @description `Allergens`: codes of the EU's fourteen (`gluten`, `crustaceans`, `eggs`, `fish`,
         *     `peanuts`, `soybeans`, `milk`, `nuts`, `celery`, `mustard`, `sesame`,
         *     `sulphites`, `lupin`, `molluscs`); `null` when not declared, `[]` for none.
         *     `DietaryLabels`: `vegetarian`, `vegan`, `glutenFree`.
         */
        CreateItemRequest: {
            /** Format: uuid */
            categoryId: string;
            name: {
                [key: string]: string;
            };
            description: null | {
                [key: string]: string;
            };
            /** Format: double */
            price: number;
            allergens?: null | string[];
            dietaryLabels?: null | string[];
        };
        CurrentTenantResponse: {
            /** Format: uuid */
            id: string;
            slug: string;
            name: string;
            defaultCulture: string;
            supportedCultures: string[];
            currency: string;
            timeZone: string;
            logoPath: null | string;
            /** Format: uri */
            logoUrl: null | string;
            accentColor: null | string;
        };
        /**
         * @description The signed-in person. `UserName` is how a user-name account signs in, and null for e-mail accounts, whose
         *     `Email` is then a real address; `IsPlatformAdmin` says whether the platform pages are open to them.
         */
        CurrentUserResponse: {
            /** Format: uuid */
            id: string;
            email: string;
            emailVerified: boolean;
            fullName: string;
            role: string;
            userName: null | string;
            isPlatformAdmin: boolean;
            tenant: components["schemas"]["CurrentTenantResponse"];
        };
        DailyMenuStatistics: {
            /** Format: date */
            day: string;
            /** Format: int64 */
            menuViews: number;
            /** Format: int64 */
            dishOpens: number;
            /** Format: int64 */
            arStarts: number;
        };
        DeleteAccountRequest: {
            password: string;
            language: null | string;
        };
        /** @description A dish's activity, most opened first. Deleted dishes are left out. */
        DishStatistics: {
            /** Format: uuid */
            itemId: string;
            name: string;
            /** Format: int64 */
            opens: number;
            /** Format: int64 */
            modelViews: number;
            /** Format: int64 */
            arStarts: number;
        };
        EmailVerificationRequest: {
            token: string;
        };
        /**
         * @description A bearer token for a business the administrator entered, and the business's slug. There is no refresh token:
         *     when it expires, the dashboard asks to enter again.
         */
        EnteredBusinessResponse: {
            accessToken: string;
            /** Format: date-time */
            expiresAt: string;
            /** Format: int32 */
            expiresIn: number;
            workspace: string;
            tokenType?: null | string;
        };
        /**
         * @description A recorded field. Menu items: `name`, `description` (translations), `price` (amount),
         *     `category` (id), `photo`, `model` (present or not), `visible`, `available`. Categories:
         *     `name`, `description`, `visible`. Business: `name`, `defaultLanguage`, `languages`,
         *     `timeZone`. Members: `role`.
         */
        HistoryChange: {
            field: string;
            before: null | components["schemas"]["JsonNode"];
            after: null | components["schemas"]["JsonNode"];
        };
        /**
         * @description One change. `Action` is `created`, `updated`, `deleted` or `reordered`; `Actor` is
         *     `null` when the system made the change (e.g. 3D model processing).
         */
        HistoryEntryResponse: {
            /** Format: uuid */
            id: string;
            /** Format: date-time */
            occurredAt: string;
            actor: null | components["schemas"]["HistoryPerson"];
            action: string;
            subject: components["schemas"]["HistorySubject"];
            changes: components["schemas"]["HistoryChange"][];
        };
        HistoryPage: {
            entries: components["schemas"]["HistoryEntryResponse"][];
            /** Format: uuid */
            nextCursor: null | string;
        };
        /** @description An account; `FullName` is `null` once the person deleted it. */
        HistoryPerson: {
            /** Format: uuid */
            id: string;
            fullName: null | string;
        };
        /**
         * @description What changed. `Type` is `menu_item`, `menu_category`, `menu`, `business` or `member`.
         *     Menu items and categories carry their name in every language at the time of the change; members carry the person.
         */
        HistorySubject: {
            type: string;
            /** Format: uuid */
            id: string;
            name: null | {
                [key: string]: string;
            };
            person: null | components["schemas"]["HistoryPerson"];
        };
        HttpValidationProblemDetails: {
            type?: null | string;
            title?: null | string;
            /** Format: int32 */
            status?: null | number;
            detail?: null | string;
            instance?: null | string;
            errors?: {
                [key: string]: string[];
            };
            /** @description Stable, machine-readable error code, e.g. `tenant.not_found`. */
            code?: string;
            /** @description Correlates the response with server logs and traces. */
            traceId?: string;
            /** @description Stable error codes per field (JSON path), for localized messages on the client. */
            errorCodes?: {
                [key: string]: string[];
            };
        };
        InvitationRequest: {
            email: string;
            role: components["schemas"]["TeamRole"];
            language: null | string;
        };
        /**
         * @description An invitation as its holder sees it, including whether the address already has an account, to be confirmed with
         *     its password.
         */
        InvitationResponse: {
            businessName: string;
            email: string;
            role: components["schemas"]["TeamRole"];
            /** Format: date-time */
            expiresAt: string;
            hasAccount: boolean;
        };
        /**
         * @description A saved invitation, pending whether or not the e-mail got out. When the e-mail was not sent, the mail server
         *     did not accept it, and the invitation can be sent again.
         */
        InvitationSent: {
            /** Format: uuid */
            invitationId: string;
            emailSent: boolean;
        };
        InvitationTokenRequest: {
            token: string;
        };
        JsonNode: unknown;
        LanguagesRequest: {
            defaultCulture: string;
            supportedCultures: string[];
        };
        ManagedArModelResponse: {
            glbPath: string;
            sceneViewerGlbPath: null | string;
            usdzPath: null | string;
            posterPath: null | string;
            /** Format: uri */
            glbUrl: string;
            /** Format: uri */
            sceneViewerGlbUrl: null | string;
            /** Format: uri */
            usdzUrl: null | string;
            /** Format: uri */
            posterUrl: null | string;
        };
        ManagedMenuCategoryResponse: {
            /** Format: uuid */
            id: string;
            name: {
                [key: string]: string;
            };
            description: null | {
                [key: string]: string;
            };
            /** Format: int32 */
            displayOrder: number;
            isVisible: boolean;
            items: components["schemas"]["ManagedMenuItemResponse"][];
        };
        ManagedMenuItemResponse: {
            /** Format: uuid */
            id: string;
            name: {
                [key: string]: string;
            };
            description: null | {
                [key: string]: string;
            };
            /** Format: double */
            price: number;
            /** Format: int32 */
            displayOrder: number;
            isVisible: boolean;
            isAvailable: boolean;
            arModel: null | components["schemas"]["ManagedArModelResponse"];
            allergens: null | string[];
            dietaryLabels: string[];
        };
        ManagedMenuResponse: {
            categories: components["schemas"]["ManagedMenuCategoryResponse"][];
        };
        MemberAddedResponse: {
            /** Format: uuid */
            membershipId: string;
        };
        MemberPasswordRequest: {
            password: string;
        };
        MenuEventInput: {
            type: components["schemas"]["MenuEventType"];
            /** Format: uuid */
            itemId: null | string;
        };
        MenuEventsRequest: {
            events: components["schemas"]["MenuEventInput"][];
        };
        /**
         * @description What a guest did. Counted per day, never stored per guest.
         * @enum {unknown}
         */
        MenuEventType: "menu_viewed" | "dish_opened" | "model_viewed" | "ar_started";
        /** @description A dish the file adds (`created`) or changes (`updated`), with the columns that change. */
        MenuImportChange: {
            /** Format: int32 */
            line: number;
            /** Format: uuid */
            itemId: string;
            name: string;
            kind: string;
            fields: string[];
        };
        /** @description A problem at a line (1 is the header) and column of the file; `Column` is null for the whole row. */
        MenuImportError: {
            /** Format: int32 */
            line: number;
            column: null | string;
            code: string;
            message: string;
        };
        /** @description What the file did or would do. `Applied` is false for a dry run and whenever `Errors` is not empty. */
        MenuImportResponse: {
            applied: boolean;
            /** Format: int32 */
            created: number;
            /** Format: int32 */
            updated: number;
            /** Format: int32 */
            unchanged: number;
            changes: components["schemas"]["MenuImportChange"][];
            errors: components["schemas"]["MenuImportError"][];
        };
        MenuStatisticsResponse: {
            /** Format: date */
            from: string;
            /** Format: date */
            to: string;
            totals: components["schemas"]["MenuStatisticsTotals"];
            days: components["schemas"]["DailyMenuStatistics"][];
            dishes: components["schemas"]["DishStatistics"][];
        };
        MenuStatisticsTotals: {
            /** Format: int64 */
            menuViews: number;
            /** Format: int64 */
            dishOpens: number;
            /** Format: int64 */
            modelViews: number;
            /** Format: int64 */
            arStarts: number;
        };
        ModelDimensionsResponse: {
            /** Format: double */
            width: number;
            /** Format: double */
            height: number;
            /** Format: double */
            depth: number;
        };
        ModelFileSizesResponse: {
            /** Format: int64 */
            source: number;
            /** Format: int64 */
            model: number;
            /** Format: int64 */
            sceneViewerModel: number;
            /** Format: int64 */
            appleModel: number;
            /** Format: int64 */
            poster: number;
        };
        ModelStatisticsResponse: {
            /** Format: int32 */
            triangles: number;
            /** Format: int32 */
            vertices: number;
            /** Format: int32 */
            materials: number;
            /** Format: int32 */
            textures: number;
            /** Format: int32 */
            maxTextureSize: number;
        };
        NewPasswordRequest: {
            token: string;
            password: string;
        };
        OpenBusinessRequest: {
            businessName: string;
            slug: string;
            defaultCulture: string;
            currency: string;
            ownerFullName: string;
            ownerUserName: string;
            ownerPassword: string;
            timeZone: null | string;
        };
        OpenedBusinessResponse: {
            /** Format: uuid */
            id: string;
            slug: string;
        };
        PasswordConfirmationRequest: {
            password: string;
        };
        PasswordResetRequest: {
            email: string;
            language: null | string;
        };
        /** @description A pending invitation. Once expired, its link no longer works; sending again makes a new one. */
        PendingInvitationResponse: {
            /** Format: uuid */
            id: string;
            email: string;
            role: components["schemas"]["TeamRole"];
            invitedBy: string;
            /** Format: date-time */
            sentAt: string;
            /** Format: date-time */
            expiresAt: string;
            isExpired: boolean;
        };
        ProblemDetails: {
            type?: null | string;
            title?: null | string;
            /** Format: int32 */
            status?: null | number;
            detail?: null | string;
            instance?: null | string;
            /** @description Stable, machine-readable error code, e.g. `tenant.not_found`. */
            code?: string;
            /** @description Correlates the response with server logs and traces. */
            traceId?: string;
        };
        PublicMenuCategoryResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            description: null | string;
            items: components["schemas"]["PublicMenuItemResponse"][];
        };
        /**
         * @description A dish as guests see it. `Allergens` is `null` when the business has not declared them, which a
         *     guest must never read as "contains none"; an empty list does say that.
         */
        PublicMenuItemResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            description: null | string;
            /** Format: double */
            price: number;
            isAvailable: boolean;
            arModel: null | components["schemas"]["ArModelResponse"];
            allergens: null | string[];
            dietaryLabels: string[];
        };
        PublicMenuResponse: {
            tenant: components["schemas"]["PublicTenantResponse"];
            culture: string;
            categories: components["schemas"]["PublicMenuCategoryResponse"][];
        };
        /**
         * @description The business the menu belongs to. `LogoUrl` and `AccentColor` (`#rrggbb`) are `null`
         *     when the business has not styled its menu; `OnAccentColor` is the text colour to draw on `AccentColor`,
         *     decided here so every client reads the same, legible pairing.
         */
        PublicTenantResponse: {
            name: string;
            slug: string;
            currency: string;
            defaultCulture: string;
            supportedCultures: string[];
            /** Format: uri */
            logoUrl: null | string;
            accentColor: null | string;
            onAccentColor: null | string;
        };
        PublishedAsset: {
            path: string;
            /** Format: uri */
            url: string;
            contentType: string;
            /** Format: int64 */
            size: number;
        };
        PublishRequest: {
            kind: components["schemas"]["AssetKind"];
        };
        ReorderRequest: {
            ids: string[];
        };
        ResendRequest: {
            language: null | string;
        };
        ResendVerificationRequest: {
            language: null | string;
        };
        RoleRequest: {
            role: components["schemas"]["TeamRole"];
        };
        SignInRequest: {
            email: string;
            password: string;
        };
        /**
         * @description A team member. `UserName` is set for accounts that sign in with one, whose `Email` is then only a
         *     placeholder no mail reaches; those are also the accounts whose password the owner can reset.
         */
        TeamMemberResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            userId: string;
            fullName: string;
            email: string;
            role: components["schemas"]["TeamRole"];
            /** Format: date-time */
            joinedAt: string;
            /** Format: date-time */
            lastSignedInAt: null | string;
            userName?: null | string;
        };
        TeamResponse: {
            members: components["schemas"]["TeamMemberResponse"][];
            invitations: components["schemas"]["PendingInvitationResponse"][];
        };
        /**
         * @description A member's role as the API names it, the same names access tokens carry.
         * @enum {unknown}
         */
        TeamRole: "Owner" | "Manager" | "Staff";
        TimeZoneRequest: {
            timeZone: string;
        };
        /** @description Replaces the item's details; see CreateItemRequest for allergens and dietary labels. */
        UpdateItemRequest: {
            /** Format: uuid */
            categoryId: string;
            name: {
                [key: string]: string;
            };
            description: null | {
                [key: string]: string;
            };
            /** Format: double */
            price: number;
            isVisible: boolean;
            allergens?: null | string[];
            dietaryLabels?: null | string[];
        };
        UploadRequest: {
            kind: components["schemas"]["AssetKind"];
            contentType: string;
            /** Format: int64 */
            size: number;
        };
        UserNameSignInRequest: {
            userName: string;
            password: string;
        };
        /**
         * @description A token response that also says where the session lives: `Workspace` is the slug whose auth endpoints
         *     refresh and end it, and `IsPlatformAdmin` is true for the administrator, whose workspace is the platform.
         */
        WorkspaceAccessTokenResponse: {
            accessToken: string;
            /** Format: date-time */
            expiresAt: string;
            /** Format: int32 */
            expiresIn: number;
            workspace: string;
            isPlatformAdmin: boolean;
            tokenType?: null | string;
        };
        WorkspaceResponse: {
            slug: string;
            name: string;
            role: components["schemas"]["TeamRole"];
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type AcceptInvitationRequest = components['schemas']['AcceptInvitationRequest'];
export type AccessTokenResponse = components['schemas']['AccessTokenResponse'];
export type AccountDeletionResponse = components['schemas']['AccountDeletionResponse'];
export type AddMemberRequest = components['schemas']['AddMemberRequest'];
export type AffectedWorkspace = components['schemas']['AffectedWorkspace'];
export type ArModelProcessingRequest = components['schemas']['ArModelProcessingRequest'];
export type ArModelProcessingResponse = components['schemas']['ArModelProcessingResponse'];
export type ArModelProcessingState = components['schemas']['ArModelProcessingState'];
export type ArModelReportResponse = components['schemas']['ArModelReportResponse'];
export type ArModelRequest = components['schemas']['ArModelRequest'];
export type ArModelResponse = components['schemas']['ArModelResponse'];
export type AssetKind = components['schemas']['AssetKind'];
export type AssetUpload = components['schemas']['AssetUpload'];
export type AvailabilityRequest = components['schemas']['AvailabilityRequest'];
export type BrandingRequest = components['schemas']['BrandingRequest'];
export type BusinessSummary = components['schemas']['BusinessSummary'];
export type CategoryRequest = components['schemas']['CategoryRequest'];
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest'];
export type CreatedResponse = components['schemas']['CreatedResponse'];
export type CreateItemRequest = components['schemas']['CreateItemRequest'];
export type CurrentTenantResponse = components['schemas']['CurrentTenantResponse'];
export type CurrentUserResponse = components['schemas']['CurrentUserResponse'];
export type DailyMenuStatistics = components['schemas']['DailyMenuStatistics'];
export type DeleteAccountRequest = components['schemas']['DeleteAccountRequest'];
export type DishStatistics = components['schemas']['DishStatistics'];
export type EmailVerificationRequest = components['schemas']['EmailVerificationRequest'];
export type EnteredBusinessResponse = components['schemas']['EnteredBusinessResponse'];
export type HistoryChange = components['schemas']['HistoryChange'];
export type HistoryEntryResponse = components['schemas']['HistoryEntryResponse'];
export type HistoryPage = components['schemas']['HistoryPage'];
export type HistoryPerson = components['schemas']['HistoryPerson'];
export type HistorySubject = components['schemas']['HistorySubject'];
export type HttpValidationProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type InvitationRequest = components['schemas']['InvitationRequest'];
export type InvitationResponse = components['schemas']['InvitationResponse'];
export type InvitationSent = components['schemas']['InvitationSent'];
export type InvitationTokenRequest = components['schemas']['InvitationTokenRequest'];
export type JsonNode = components['schemas']['JsonNode'];
export type LanguagesRequest = components['schemas']['LanguagesRequest'];
export type ManagedArModelResponse = components['schemas']['ManagedArModelResponse'];
export type ManagedMenuCategoryResponse = components['schemas']['ManagedMenuCategoryResponse'];
export type ManagedMenuItemResponse = components['schemas']['ManagedMenuItemResponse'];
export type ManagedMenuResponse = components['schemas']['ManagedMenuResponse'];
export type MemberAddedResponse = components['schemas']['MemberAddedResponse'];
export type MemberPasswordRequest = components['schemas']['MemberPasswordRequest'];
export type MenuEventInput = components['schemas']['MenuEventInput'];
export type MenuEventsRequest = components['schemas']['MenuEventsRequest'];
export type MenuEventType = components['schemas']['MenuEventType'];
export type MenuImportChange = components['schemas']['MenuImportChange'];
export type MenuImportError = components['schemas']['MenuImportError'];
export type MenuImportResponse = components['schemas']['MenuImportResponse'];
export type MenuStatisticsResponse = components['schemas']['MenuStatisticsResponse'];
export type MenuStatisticsTotals = components['schemas']['MenuStatisticsTotals'];
export type ModelDimensionsResponse = components['schemas']['ModelDimensionsResponse'];
export type ModelFileSizesResponse = components['schemas']['ModelFileSizesResponse'];
export type ModelStatisticsResponse = components['schemas']['ModelStatisticsResponse'];
export type NewPasswordRequest = components['schemas']['NewPasswordRequest'];
export type OpenBusinessRequest = components['schemas']['OpenBusinessRequest'];
export type OpenedBusinessResponse = components['schemas']['OpenedBusinessResponse'];
export type PasswordConfirmationRequest = components['schemas']['PasswordConfirmationRequest'];
export type PasswordResetRequest = components['schemas']['PasswordResetRequest'];
export type PendingInvitationResponse = components['schemas']['PendingInvitationResponse'];
export type ProblemDetails = components['schemas']['ProblemDetails'];
export type PublicMenuCategoryResponse = components['schemas']['PublicMenuCategoryResponse'];
export type PublicMenuItemResponse = components['schemas']['PublicMenuItemResponse'];
export type PublicMenuResponse = components['schemas']['PublicMenuResponse'];
export type PublicTenantResponse = components['schemas']['PublicTenantResponse'];
export type PublishedAsset = components['schemas']['PublishedAsset'];
export type PublishRequest = components['schemas']['PublishRequest'];
export type ReorderRequest = components['schemas']['ReorderRequest'];
export type ResendRequest = components['schemas']['ResendRequest'];
export type ResendVerificationRequest = components['schemas']['ResendVerificationRequest'];
export type RoleRequest = components['schemas']['RoleRequest'];
export type SignInRequest = components['schemas']['SignInRequest'];
export type TeamMemberResponse = components['schemas']['TeamMemberResponse'];
export type TeamResponse = components['schemas']['TeamResponse'];
export type TeamRole = components['schemas']['TeamRole'];
export type TimeZoneRequest = components['schemas']['TimeZoneRequest'];
export type UpdateItemRequest = components['schemas']['UpdateItemRequest'];
export type UploadRequest = components['schemas']['UploadRequest'];
export type UserNameSignInRequest = components['schemas']['UserNameSignInRequest'];
export type WorkspaceAccessTokenResponse = components['schemas']['WorkspaceAccessTokenResponse'];
export type WorkspaceResponse = components['schemas']['WorkspaceResponse'];
export type $defs = Record<string, never>;
export interface operations {
    GetArModelProcessing: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                itemId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ArModelProcessingResponse"];
                };
            };
            /** @description Bad Request */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            /** @description Unauthorized */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
            /** @description Forbidden */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
            /** @description Not Found */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
}
