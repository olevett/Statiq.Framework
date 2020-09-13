﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Statiq.Common;

namespace Statiq.Scriban
{
    public class StatiqTemplateContext : TemplateContext
    {
        protected override IObjectAccessor GetMemberAccessorImpl(object target)
        {
            if (target is IDocument)
            {
                return new DocumentAccessor(MemberRenamer);
            }

            return base.GetMemberAccessorImpl(target);
        }

        private class DocumentAccessor : IObjectAccessor
        {
            // Built-in properties we want to expose but aren't available via metadata.
            private static readonly string[] PropertyNames =
            {
                nameof(IDocument.Id),
                nameof(IDocument.Count),
                nameof(IDocument.Keys),
                nameof(IDocument.Values)
            };

            private readonly ConcurrentDictionary<IDocument, ImmutableDictionary<string, string>> _documentMetadataCache;
            private readonly ImmutableDictionary<string, string> _properties;
            private readonly MemberRenamerDelegate _renamer;

            public DocumentAccessor(MemberRenamerDelegate memberRenamer)
            {
                _renamer = memberRenamer;
                _documentMetadataCache = new ConcurrentDictionary<IDocument, ImmutableDictionary<string, string>>(DocumentIdComparer.Instance);
                _properties = PropertyNames
                    .ToImmutableDictionary(Rename);
            }

            public int GetMemberCount(TemplateContext context, SourceSpan span, object target) => _properties.Count + GetMetadata(target as IDocument).Count;

            public IEnumerable<string> GetMembers(TemplateContext context, SourceSpan span, object target) =>
                _properties.Keys.Concat(GetMetadata(target as IDocument).Keys).Distinct();

            public bool HasMember(TemplateContext context, SourceSpan span, object target, string member) =>
                GetMetadata(target as IDocument).ContainsKey(member) || _properties.ContainsKey(member);

            public bool TryGetValue(TemplateContext context, SourceSpan span, object target, string member, out object value)
            {
                value = null;

                if (!(target is IDocument document))
                {
                    return false;
                }

                if (GetMetadata(document).TryGetValue(member, out string metadataName))
                {
                    return document.TryGetValue(metadataName, out value);
                }

                if (_properties.TryGetValue(member, out string propertyName))
                {
                    value = propertyName switch
                    {
                        nameof(IDocument.Id) => document.Id,
                        nameof(IDocument.Count) => document.Count,
                        nameof(IDocument.Keys) => document.Keys.Select(Rename),
                        nameof(IDocument.Values) => document.Values,
                        _ => null
                    };

                    return true;
                }

                return false;
            }

            public bool TrySetValue(TemplateContext context, SourceSpan span, object target, string member, object value) => false;

            private ImmutableDictionary<string, string> GetMetadata(IDocument document)
            {
                if (document is null)
                {
                    return ImmutableDictionary<string, string>.Empty;
                }

                return _documentMetadataCache
                    .GetOrAdd(document, x => x.Keys.ToImmutableDictionary(Rename));
            }

            private string Rename(string member) => _renamer?.Invoke(new DocumentMemberInfo(member)) ?? member;
        }
    }
}